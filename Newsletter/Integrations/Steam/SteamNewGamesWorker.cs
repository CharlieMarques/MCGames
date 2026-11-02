using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newsletter.Data;

namespace Newsletter.Integrations.Steam
{
    /// <summary>
    /// Revisa cada hora si ya pasaron IntervalDays desde la última búsqueda de juegos nuevos en Steam.
    /// Si la última corrida no terminó (tope o errores), la fecha no avanzó y la revisión siguiente la retoma.
    /// </summary>
    public class SteamNewGamesWorker : BackgroundService
    {
        private const int SyncStateId = 1; // misma fila que usa SteamNewGamesService
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptionsMonitor<SteamNewGamesOptions> _options;
        private readonly TimeProvider _time;
        private readonly ILogger<SteamNewGamesWorker> _logger;
        private bool _missingKeyLogged;

        public SteamNewGamesWorker(
            IServiceScopeFactory scopeFactory,
            IOptionsMonitor<SteamNewGamesOptions> options,
            TimeProvider time,
            ILogger<SteamNewGamesWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _time = time;
            _logger = logger;
        }

        public static bool IsDue(DateTime? lastRunUtc, DateTime nowUtc, int intervalDays) =>
            lastRunUtc == null || nowUtc - lastRunUtc.Value >= TimeSpan.FromDays(intervalDays);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Espera inicial para no arrancar junto con SteamCronService ni con el inicio de la app.
                await Task.Delay(StartupDelay, _time, stoppingToken);

                using var timer = new PeriodicTimer(CheckInterval, _time);
                do
                {
                    await RunIfDueAsync(stoppingToken);
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // La app se está cerrando.
            }
        }

        /// <summary>Ejecuta la búsqueda si corresponde. Devuelve true si la ejecutó.</summary>
        public async Task<bool> RunIfDueAsync(CancellationToken cancellationToken)
        {
            SteamNewGamesOptions options;
            try
            {
                options = _options.CurrentValue;
            }
            catch (OptionsValidationException ex)
            {
                _logger.LogError(ex, "Steam: la configuración SteamNewGames es inválida; la búsqueda no va a correr.");
                return false;
            }
            if (!options.Enabled)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                if (!_missingKeyLogged)
                {
                    _logger.LogWarning("Steam: falta SteamNewGames:ApiKey; la búsqueda de juegos nuevos no va a correr.");
                    _missingKeyLogged = true;
                }
                return false;
            }
            _missingKeyLogged = false;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NewsletterDbContext>();

                var lastRun = await db.SteamSyncStates
                    .Where(s => s.Id == SyncStateId)
                    .Select(s => s.LastRunUtc)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!IsDue(lastRun, _time.GetUtcNow().UtcDateTime, options.IntervalDays))
                {
                    return false;
                }

                _logger.LogInformation("Steam: arranca la búsqueda de juegos nuevos (última corrida: {LastRun})", lastRun);
                var service = scope.ServiceProvider.GetRequiredService<ISteamNewGamesService>();
                await service.RunAsync(cancellationToken);
                return true;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Steam: la búsqueda de juegos nuevos falló; se reintenta en la próxima revisión.");
                return false;
            }
        }
    }
}
