using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newsletter.Data;
using Newsletter.Models;

namespace Newsletter.Integrations.Steam
{
    public class SteamNewGamesService : ISteamNewGamesService
    {
        private const int MaxNameLength = 100;
        private const int MaxDescriptionLength = 600;
        private const int MaxLookupNameLength = 50;
        private const int MaxConsecutiveErrors = 3;
        private const int SyncStateId = 1;

        private enum SkipReason { None, NoData, NotAGame, ComingSoon }

        private readonly NewsletterDbContext _db;
        private readonly ISteamStoreClient _steam;
        private readonly SteamNewGamesOptions _options;
        private readonly TimeProvider _time;
        private readonly ILogger<SteamNewGamesService> _logger;

        private Dictionary<string, Genre> _genres = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Category> _categories = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Platform> _platforms = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _missingPlatformsLogged = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Guid> _unlinkedByName = new(StringComparer.OrdinalIgnoreCase);

        public SteamNewGamesService(
            NewsletterDbContext db,
            ISteamStoreClient steam,
            IOptions<SteamNewGamesOptions> options,
            TimeProvider time,
            ILogger<SteamNewGamesService> logger)
        {
            _db = db;
            _steam = steam;
            _options = options.Value;
            _time = time;
            _logger = logger;
        }

        public async Task<SteamSyncReport> RunAsync(CancellationToken cancellationToken = default)
        {
            var runStart = _time.GetUtcNow().UtcDateTime;

            var lastRun = await _db.SteamSyncStates
                .Where(s => s.Id == SyncStateId)
                .Select(s => s.LastRunUtc)
                .FirstOrDefaultAsync(cancellationToken);
            var since = lastRun ?? runStart.AddDays(-_options.IntervalDays);
            var report = new SteamSyncReport { SinceUtc = since };

            // 1. Candidatos: apps modificados desde la última corrida que todavía no están en Games.
            var existingIds = (await _db.Games
                    .Where(g => g.SteamAppId != null)
                    .Select(g => g.SteamAppId!.Value)
                    .ToListAsync(cancellationToken))
                .ToHashSet();

            // Juegos sin SteamAppId (los que cargó el bot de Epic), por nombre, para vincularlos en vez de duplicarlos.
            var unlinked = await _db.Games
                .Where(g => g.SteamAppId == null)
                .Select(g => new { g.Id, g.Name })
                .ToListAsync(cancellationToken);
            _unlinkedByName = unlinked
                .GroupBy(g => g.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            var seen = new HashSet<int>();
            var pending = new List<StoreApp>();
            await foreach (var app in _steam.GetModifiedAppsAsync(since, cancellationToken))
            {
                if (!existingIds.Contains(app.AppId) && seen.Add(app.AppId))
                {
                    pending.Add(app);
                }
            }
            report.Candidates = pending.Count;
            pending = pending.OrderByDescending(a => a.AppId).ToList(); // los más nuevos primero

            await LoadLookupsAsync(cancellationToken);

            // 2. Detalles e inserción, de a un juego por vez.
            int requests = 0;
            int consecutiveErrors = 0;
            foreach (var app in pending)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (requests >= _options.MaxPerRun)
                {
                    report.ReachedLimit = true;
                    break;
                }
                if (requests > 0 && _options.RequestDelayMs > 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(_options.RequestDelayMs), _time, cancellationToken);
                }
                requests++;

                SteamGameData? data;
                try
                {
                    data = await _steam.GetAppDetailsAsync(app.AppId, cancellationToken);
                    consecutiveErrors = 0;
                }
                catch (Exception ex) when (IsSteamError(ex, cancellationToken))
                {
                    report.Failed++;
                    consecutiveErrors++;
                    _logger.LogWarning(ex, "Steam: falló el pedido de detalles del app {AppId}", app.AppId);
                    if (consecutiveErrors >= MaxConsecutiveErrors)
                    {
                        report.StoppedByErrors = true;
                        break;
                    }
                    continue;
                }

                var (game, reason) = BuildGame(app.AppId, data, _time.GetUtcNow().UtcDateTime);
                if (game == null)
                {
                    switch (reason)
                    {
                        case SkipReason.ComingSoon: report.SkippedComingSoon++; break;
                        case SkipReason.NotAGame: report.SkippedNotAGame++; break;
                        default: report.SkippedNoData++; break;
                    }
                    continue;
                }

                try
                {
                    var unlinkedKey = data!.Name.Trim();
                    if (_unlinkedByName.TryGetValue(unlinkedKey, out var unlinkedId)
                        && await LinkToExistingAsync(unlinkedId, app.AppId, game, cancellationToken))
                    {
                        _unlinkedByName.Remove(unlinkedKey);
                        report.Linked++;
                    }
                    else
                    {
                        _db.Games.Add(game);
                        await _db.SaveChangesAsync(cancellationToken);
                        report.Inserted++;
                    }
                }
                catch (DbUpdateException ex)
                {
                    report.Failed++;
                    _logger.LogError(ex, "Steam: no se pudo guardar el app {AppId}", app.AppId);
                    _db.ChangeTracker.Clear();
                    await LoadLookupsAsync(cancellationToken);
                }
            }

            // 3. La fecha solo avanza si se revisaron todos los candidatos.
            if (!report.ReachedLimit && !report.StoppedByErrors)
            {
                await SaveLastRunAsync(runStart, cancellationToken);
                report.DateAdvanced = true;
            }

            _logger.LogInformation(
                "Steam: sincronización terminada. Desde {Since:u}. Candidatos {Candidates}, insertados {Inserted}, vinculados {Linked}, " +
                "próximos {ComingSoon}, no juegos {NotAGame}, sin datos {NoData}, fallidos {Failed}, " +
                "tope {Limit}, cortada por errores {Stopped}, fecha avanzada {Advanced}",
                since, report.Candidates, report.Inserted, report.Linked, report.SkippedComingSoon, report.SkippedNotAGame,
                report.SkippedNoData, report.Failed, report.ReachedLimit, report.StoppedByErrors, report.DateAdvanced);

            return report;
        }

        private static bool IsSteamError(Exception ex, CancellationToken cancellationToken) =>
            ex is SteamRateLimitException or HttpRequestException or JsonException
            || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested);

        private (Game? Game, SkipReason Reason) BuildGame(int appId, SteamGameData? data, DateTime nowUtc)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Name))
            {
                return (null, SkipReason.NoData);
            }
            if (!string.IsNullOrEmpty(data.Type) && !data.Type.Equals("game", StringComparison.OrdinalIgnoreCase))
            {
                return (null, SkipReason.NotAGame);
            }
            if (data.ReleaseDate?.ComingSoon == true)
            {
                return (null, SkipReason.ComingSoon);
            }
            if (!SteamDateParser.TryParse(data.ReleaseDate?.Date, out var releaseDate))
            {
                return (null, SkipReason.NoData);
            }

            var game = new Game
            {
                Id = Guid.NewGuid(),
                Name = Truncate(data.Name.Trim(), MaxNameLength),
                ShortDescription = Truncate(data.ShortDescription ?? string.Empty, MaxDescriptionLength),
                ReleaseDate = releaseDate,
                GameCoverUrl = data.HeaderImage ?? string.Empty,
                State = true,
                SteamAppId = appId,
                LastPriceCheck = nowUtc,
                GamePlatforms = new List<GamePlatform>(),
                GameGenre = new List<GameGenre>(),
                GameCategories = new List<GameCategory>()
            };

            ApplyPrice(game, data);

            foreach (var steamGenre in data.Genres ?? new List<SteamGenreDto>())
            {
                var genre = GetOrCreateGenre(steamGenre.Description);
                if (genre != null && game.GameGenre.All(gg => gg.Genre != genre))
                {
                    game.GameGenre.Add(new GameGenre { Genre = genre });
                }
            }

            foreach (var steamCategory in data.Categories ?? new List<SteamCategoryDto>())
            {
                var category = GetOrCreateCategory(steamCategory.Description);
                if (category != null && game.GameCategories.All(gc => gc.Category != category))
                {
                    game.GameCategories.Add(new GameCategory { Category = category });
                }
            }

            var platformNames = new List<string> { "Steam" };
            if (data.Platforms?.Windows == true) platformNames.Add("Windows");
            if (data.Platforms?.Linux == true) platformNames.Add("Linux");
            if (data.Platforms?.Mac == true) platformNames.Add("Mac");
            foreach (var name in platformNames)
            {
                if (_platforms.TryGetValue(name, out var platform))
                {
                    game.GamePlatforms.Add(new GamePlatform { Platform = platform, State = true, AddedDate = nowUtc });
                }
                else if (_missingPlatformsLogged.Add(name))
                {
                    _logger.LogWarning("Steam: la plataforma {Platform} no existe en la tabla Platforms; se omite", name);
                }
            }

            return (game, SkipReason.None);
        }

        /// <summary>
        /// Completa con los datos de Steam un juego que ya existe sin SteamAppId (cargado desde Epic).
        /// No toca nombre, descripción, portada ni géneros. Devuelve false si el juego ya no está disponible para vincular.
        /// </summary>
        private async Task<bool> LinkToExistingAsync(Guid gameId, int appId, Game steamGame, CancellationToken cancellationToken)
        {
            var existing = await _db.Games
                .Include(g => g.GamePlatforms)
                .FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);
            if (existing == null || existing.SteamAppId != null)
            {
                return false;
            }

            existing.SteamAppId = appId;
            existing.Price = steamGame.Price;
            existing.FinalPrice = steamGame.FinalPrice;
            existing.DiscountPercentage = steamGame.DiscountPercentage;
            existing.OnOffer = steamGame.OnOffer;
            existing.LastPriceCheck = steamGame.LastPriceCheck;
            existing.ReleaseDate ??= steamGame.ReleaseDate;

            existing.GamePlatforms ??= new List<GamePlatform>();
            foreach (var steamPlatform in steamGame.GamePlatforms ?? new List<GamePlatform>())
            {
                var platform = steamPlatform.Platform!;
                if (existing.GamePlatforms.All(gp => gp.PlatformId != platform.Id))
                {
                    existing.GamePlatforms.Add(new GamePlatform { Platform = platform, State = true, AddedDate = steamPlatform.AddedDate });
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static void ApplyPrice(Game game, SteamGameData data)
        {
            if (data.IsFree || data.PriceOverview == null)
            {
                game.Price = 0;
                game.FinalPrice = 0;
                game.DiscountPercentage = 0;
                game.OnOffer = false;
                return;
            }

            game.Price = data.PriceOverview.Initial / 100m;
            game.FinalPrice = data.PriceOverview.Final / 100m;
            game.DiscountPercentage = data.PriceOverview.DiscountPercent;
            game.OnOffer = data.PriceOverview.DiscountPercent > 0;
        }

        private Genre? GetOrCreateGenre(string? description)
        {
            var name = NormalizeLookupName(description);
            if (name == null)
            {
                return null;
            }
            if (!_genres.TryGetValue(name, out var genre))
            {
                genre = new Genre { Description = name, State = true };
                _genres[name] = genre;
            }
            return genre;
        }

        private Category? GetOrCreateCategory(string? description)
        {
            var name = NormalizeLookupName(description);
            if (name == null)
            {
                return null;
            }
            if (!_categories.TryGetValue(name, out var category))
            {
                category = new Category { Description = name };
                _categories[name] = category;
            }
            return category;
        }

        private static string? NormalizeLookupName(string? text)
        {
            var name = text?.Trim();
            return string.IsNullOrEmpty(name) ? null : Truncate(name, MaxLookupNameLength);
        }

        private async Task LoadLookupsAsync(CancellationToken cancellationToken)
        {
            _genres = BuildLookup(await _db.Genres.ToListAsync(cancellationToken), g => g.Description);
            _categories = BuildLookup(await _db.Set<Category>().ToListAsync(cancellationToken), c => c.Description);
            _platforms = BuildLookup(await _db.Platforms.ToListAsync(cancellationToken), p => p.Description);
        }

        private static Dictionary<string, T> BuildLookup<T>(IEnumerable<T> items, Func<T, string> nameOf) =>
            items.GroupBy(i => nameOf(i).Trim(), StringComparer.OrdinalIgnoreCase)
                 .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        private async Task SaveLastRunAsync(DateTime runStart, CancellationToken cancellationToken)
        {
            var state = await _db.SteamSyncStates.FirstOrDefaultAsync(s => s.Id == SyncStateId, cancellationToken);
            if (state == null)
            {
                _db.SteamSyncStates.Add(new SteamSyncState { Id = SyncStateId, LastRunUtc = runStart });
            }
            else
            {
                state.LastRunUtc = runStart;
            }
            await _db.SaveChangesAsync(cancellationToken);
        }

        private static string Truncate(string text, int maxLength) =>
            text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
    }
}
