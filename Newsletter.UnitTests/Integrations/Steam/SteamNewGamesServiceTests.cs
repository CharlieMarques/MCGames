using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newsletter.Data;
using Newsletter.Models;
using Newsletter.Integrations.Steam;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Newsletter.UnitTests.Integrations.Steam
{
    public class SteamNewGamesServiceTests : IDisposable
    {
        private static readonly DateTime Now = new(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);

        private readonly NewsletterDbContext _db;
        private readonly Mock<ISteamStoreClient> _steam = new();
        private readonly SteamNewGamesOptions _options = new()
        {
            IntervalDays = 7,
            MaxPerRun = 1000,
            RequestDelayMs = 0,
            ApiKey = "clave"
        };
        private DateTime? _requestedSince;

        public SteamNewGamesServiceTests()
        {
            var dbOptions = new DbContextOptionsBuilder<NewsletterDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new NewsletterDbContext(dbOptions);

            _db.Platforms.AddRange(
                new Platform { Description = "Steam", State = true },
                new Platform { Description = "Windows", State = true },
                new Platform { Description = "Linux", State = true },
                new Platform { Description = "Mac", State = true });
            _db.Genres.AddRange(
                new Genre { Description = "Acción", State = true },
                new Genre { Description = "Rol", State = true });
            _db.Set<Category>().Add(new Category { Description = "Un jugador" });
            _db.SaveChanges();
        }

        public void Dispose() => _db.Dispose();

        // ---------- Ayudas ----------

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _now;

            public FixedTimeProvider(DateTime utcNow) => _now = new DateTimeOffset(utcNow);

            public override DateTimeOffset GetUtcNow() => _now;
        }

        private SteamNewGamesService CreateService() => new(
            _db,
            _steam.Object,
            MsOptions.Create(_options),
            new FixedTimeProvider(Now),
            NullLogger<SteamNewGamesService>.Instance);

        private static SteamGameData Juego(string name = "Juego nuevo") => new()
        {
            Name = name,
            ShortDescription = "Descripción",
            HeaderImage = "https://img/header.jpg",
            Type = "game",
            IsFree = false,
            ReleaseDate = new SteamReleaseDate { ComingSoon = false, Date = "12 oct 2026" },
            PriceOverview = new SteamPriceOverview { Initial = 100000, Final = 50000, DiscountPercent = 50 },
            Genres = new List<SteamGenreDto> { new() { Id = "1", Description = "Acción" } },
            Categories = new List<SteamCategoryDto> { new() { Id = 2, Description = "Un jugador" } },
            Platforms = new SteamPlatformsDto { Windows = true, Mac = false, Linux = true }
        };

        private void SteamDevuelveApps(params int[] appIds)
        {
            _steam.Setup(s => s.GetModifiedAppsAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .Returns((DateTime? since, CancellationToken _) =>
                {
                    _requestedSince = since;
                    return ToAsync(appIds.Select(id => new StoreApp { AppId = id, Name = "App " + id }));
                });
        }

        private void SteamDevuelveDetalles(int appId, SteamGameData? data) =>
            _steam.Setup(s => s.GetAppDetailsAsync(appId, It.IsAny<CancellationToken>())).ReturnsAsync(data);

        private static async IAsyncEnumerable<StoreApp> ToAsync(IEnumerable<StoreApp> apps)
        {
            foreach (var app in apps)
            {
                yield return app;
            }
            await Task.CompletedTask;
        }

        // ---------- Guardado ----------

        [Fact]
        public async Task RunAsync_CuandoHayUnJuegoNuevo_LoGuardaConTodosLosDatos()
        {
            SteamDevuelveApps(500);
            SteamDevuelveDetalles(500, Juego());

            var report = await CreateService().RunAsync();

            Assert.Equal(1, report.Inserted);
            var game = await _db.Games
                .Include(g => g.GameGenre!).ThenInclude(gg => gg.Genre)
                .Include(g => g.GameCategories!).ThenInclude(gc => gc.Category)
                .Include(g => g.GamePlatforms!).ThenInclude(gp => gp.Platform)
                .SingleAsync(g => g.SteamAppId == 500);

            Assert.Equal("Juego nuevo", game.Name);
            Assert.Equal("Descripción", game.ShortDescription);
            Assert.Equal("https://img/header.jpg", game.GameCoverUrl);
            Assert.Equal(new DateTime(2026, 10, 12), game.ReleaseDate);
            Assert.True(game.State);
            Assert.Equal(1000m, game.Price);
            Assert.Equal(500m, game.FinalPrice);
            Assert.Equal(50m, game.DiscountPercentage);
            Assert.True(game.OnOffer);
            Assert.Equal(Now, game.LastPriceCheck);
            Assert.Equal("Acción", Assert.Single(game.GameGenre!).Genre!.Description);
            Assert.Equal("Un jugador", Assert.Single(game.GameCategories!).Category!.Description);
            Assert.Equal(
                new[] { "Linux", "Steam", "Windows" },
                game.GamePlatforms!.Select(gp => gp.Platform!.Description).OrderBy(d => d));
            Assert.All(game.GamePlatforms!, gp =>
            {
                Assert.True(gp.State);
                Assert.Equal(Now, gp.AddedDate);
            });
        }

        [Fact]
        public async Task RunAsync_CuandoElJuegoEsGratis_GuardaPreciosEnCero()
        {
            var data = Juego();
            data.IsFree = true;
            data.PriceOverview = null!;
            SteamDevuelveApps(500);
            SteamDevuelveDetalles(500, data);

            await CreateService().RunAsync();

            var game = await _db.Games.SingleAsync();
            Assert.Equal(0m, game.Price);
            Assert.Equal(0m, game.FinalPrice);
            Assert.Equal(0m, game.DiscountPercentage);
            Assert.False(game.OnOffer);
        }

        [Fact]
        public async Task RunAsync_CortaNombreYDescripcionLargos()
        {
            var data = Juego(new string('N', 150));
            data.ShortDescription = new string('D', 700);
            SteamDevuelveApps(500);
            SteamDevuelveDetalles(500, data);

            await CreateService().RunAsync();

            var game = await _db.Games.SingleAsync();
            Assert.Equal(100, game.Name.Length);
            Assert.EndsWith("...", game.Name);
            Assert.Equal(600, game.ShortDescription.Length);
        }

        [Fact]
        public async Task RunAsync_ReutilizaGenerosSinImportarMayusculas_YCreaLosQueFaltanUnaSolaVez()
        {
            var a = Juego("A");
            a.Genres = new List<SteamGenreDto>
            {
                new() { Id = "1", Description = "ACCIÓN" },
                new() { Id = "99", Description = "Género nuevo" }
            };
            var b = Juego("B");
            b.Genres = new List<SteamGenreDto> { new() { Id = "99", Description = "Género nuevo" } };
            b.Categories = new List<SteamCategoryDto> { new() { Id = 50, Description = "Categoría nueva" } };
            SteamDevuelveApps(1, 2);
            SteamDevuelveDetalles(1, a);
            SteamDevuelveDetalles(2, b);

            var report = await CreateService().RunAsync();

            Assert.Equal(2, report.Inserted);
            var genres = await _db.Genres.Select(g => g.Description).ToListAsync();
            Assert.Equal(3, genres.Count); // Acción, Rol y Género nuevo
            Assert.Single(genres, d => d == "Género nuevo");
            var categories = await _db.Set<Category>().Select(c => c.Description).ToListAsync();
            Assert.Equal(2, categories.Count); // Un jugador y Categoría nueva
        }

        // ---------- Omisiones ----------

        [Fact]
        public async Task RunAsync_CuandoElJuegoYaExiste_NoPideDetallesNiLoDuplica()
        {
            _db.Games.Add(new Game { Id = Guid.NewGuid(), Name = "Viejo", SteamAppId = 500, State = true });
            await _db.SaveChangesAsync();
            SteamDevuelveApps(500, 500);

            var report = await CreateService().RunAsync();

            Assert.Equal(0, report.Candidates);
            Assert.Equal(1, await _db.Games.CountAsync());
            _steam.Verify(s => s.GetAppDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RunAsync_OmiteProximosNoJuegosYSinDatos_YCuentaCadaCaso()
        {
            var proximo = Juego();
            proximo.ReleaseDate = new SteamReleaseDate { ComingSoon = true, Date = "Próximamente" };
            var dlc = Juego();
            dlc.Type = "dlc";
            var sinFecha = Juego();
            sinFecha.ReleaseDate = new SteamReleaseDate { ComingSoon = false, Date = "oct 2026" };
            SteamDevuelveApps(1, 2, 3, 4);
            SteamDevuelveDetalles(1, proximo);
            SteamDevuelveDetalles(2, dlc);
            SteamDevuelveDetalles(3, sinFecha);
            SteamDevuelveDetalles(4, null);

            var report = await CreateService().RunAsync();

            Assert.Equal(4, report.Candidates);
            Assert.Equal(1, report.SkippedComingSoon);
            Assert.Equal(1, report.SkippedNotAGame);
            Assert.Equal(2, report.SkippedNoData);
            Assert.Equal(0, report.Inserted);
            Assert.Empty(_db.Games);
            Assert.True(report.DateAdvanced);
        }

        // ---------- Tope y errores ----------

        [Fact]
        public async Task RunAsync_CuandoLlegaAlTope_ProcesaLosMasNuevosYNoAvanzaLaFecha()
        {
            _options.MaxPerRun = 2;
            SteamDevuelveApps(10, 30, 20);
            SteamDevuelveDetalles(10, Juego("Diez"));
            SteamDevuelveDetalles(20, Juego("Veinte"));
            SteamDevuelveDetalles(30, Juego("Treinta"));

            var report = await CreateService().RunAsync();

            Assert.True(report.ReachedLimit);
            Assert.False(report.DateAdvanced);
            var ids = await _db.Games.Select(g => g.SteamAppId!.Value).ToListAsync();
            Assert.Equal(new[] { 20, 30 }, ids.OrderBy(id => id));
            Assert.Empty(_db.SteamSyncStates);
        }

        [Fact]
        public async Task RunAsync_ConTresErroresSeguidos_CortaSinPerderLoGuardado()
        {
            SteamDevuelveApps(5, 4, 3, 2, 1);
            SteamDevuelveDetalles(5, Juego("Cinco"));
            _steam.Setup(s => s.GetAppDetailsAsync(It.IsIn(4, 3, 2), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new SteamRateLimitException());
            SteamDevuelveDetalles(1, Juego("Uno"));

            var report = await CreateService().RunAsync();

            Assert.True(report.StoppedByErrors);
            Assert.False(report.DateAdvanced);
            Assert.Equal(1, report.Inserted);
            Assert.Equal(3, report.Failed);
            Assert.Equal(5, (await _db.Games.SingleAsync()).SteamAppId);
            _steam.Verify(s => s.GetAppDetailsAsync(1, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RunAsync_ConUnErrorAislado_SigueYAvanzaLaFecha()
        {
            SteamDevuelveApps(3, 2, 1);
            SteamDevuelveDetalles(3, Juego("Tres"));
            _steam.Setup(s => s.GetAppDetailsAsync(2, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Error 500"));
            SteamDevuelveDetalles(1, Juego("Uno"));

            var report = await CreateService().RunAsync();

            Assert.Equal(2, report.Inserted);
            Assert.Equal(1, report.Failed);
            Assert.False(report.StoppedByErrors);
            Assert.True(report.DateAdvanced);
        }

        // ---------- Fecha de última corrida ----------

        [Fact]
        public async Task RunAsync_LaPrimeraVez_MiraHaciaAtrasIntervalDays_YGuardaLaFechaDeInicio()
        {
            SteamDevuelveApps();

            var report = await CreateService().RunAsync();

            Assert.Equal(Now.AddDays(-7), _requestedSince);
            Assert.True(report.DateAdvanced);
            var state = await _db.SteamSyncStates.SingleAsync();
            Assert.Equal(1, state.Id);
            Assert.Equal(Now, state.LastRunUtc);
        }

        [Fact]
        public async Task RunAsync_ConUnaCorridaAnterior_PideDesdeEsaFechaYLaActualiza()
        {
            var anterior = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
            _db.SteamSyncStates.Add(new SteamSyncState { Id = 1, LastRunUtc = anterior });
            await _db.SaveChangesAsync();
            SteamDevuelveApps();

            await CreateService().RunAsync();

            Assert.Equal(anterior, _requestedSince);
            Assert.Equal(Now, (await _db.SteamSyncStates.SingleAsync()).LastRunUtc);
        }

        // ---------- Vinculación con juegos de Epic ----------

        [Fact]
        public async Task RunAsync_CuandoHayUnJuegoDeEpicConElMismoNombre_LoVinculaEnVezDeDuplicarlo()
        {
            var epicId = Guid.NewGuid();
            _db.Games.Add(new Game
            {
                Id = epicId,
                Name = "JUEGO NUEVO",
                ShortDescription = "Descripción de Epic",
                GameCoverUrl = "https://img/epic.jpg",
                State = true,
                SteamAppId = null
            });
            await _db.SaveChangesAsync();
            SteamDevuelveApps(500);
            SteamDevuelveDetalles(500, Juego("Juego nuevo"));

            var report = await CreateService().RunAsync();

            Assert.Equal(1, report.Linked);
            Assert.Equal(0, report.Inserted);
            var game = await _db.Games
                .Include(g => g.GamePlatforms!).ThenInclude(gp => gp.Platform)
                .Include(g => g.GameGenre!)
                .SingleAsync();
            Assert.Equal(epicId, game.Id);
            Assert.Equal(500, game.SteamAppId);
            Assert.Equal("JUEGO NUEVO", game.Name);
            Assert.Equal("Descripción de Epic", game.ShortDescription);
            Assert.Equal("https://img/epic.jpg", game.GameCoverUrl);
            Assert.Equal(1000m, game.Price);
            Assert.Equal(500m, game.FinalPrice);
            Assert.True(game.OnOffer);
            Assert.Equal(
                new[] { "Linux", "Steam", "Windows" },
                game.GamePlatforms!.Select(gp => gp.Platform!.Description).OrderBy(d => d));
            Assert.True(game.GameGenre == null || game.GameGenre.Count == 0);
        }

        [Fact]
        public async Task RunAsync_CuandoElJuegoConEseNombreYaTieneOtroSteamAppId_InsertaUnoNuevo()
        {
            _db.Games.Add(new Game { Id = Guid.NewGuid(), Name = "Juego nuevo", SteamAppId = 999, State = true });
            await _db.SaveChangesAsync();
            SteamDevuelveApps(500);
            SteamDevuelveDetalles(500, Juego("Juego nuevo"));

            var report = await CreateService().RunAsync();

            Assert.Equal(0, report.Linked);
            Assert.Equal(1, report.Inserted);
            Assert.Equal(2, await _db.Games.CountAsync());
        }
    }
}
