using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Newsletter.Data;
using Newsletter.Models;
using Newsletter.Integrations.Steam;

namespace Newsletter.UnitTests.Integrations.Steam
{
    public class SteamNewGamesWorkerTests : IDisposable
    {
        private static readonly DateTime Now = new(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);

        private readonly ServiceProvider _provider;
        private readonly Mock<ISteamNewGamesService> _service = new();
        private readonly SteamNewGamesOptions _options = new() { Enabled = true, IntervalDays = 7, ApiKey = "clave" };

        public SteamNewGamesWorkerTests()
        {
            _service.Setup(s => s.RunAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SteamSyncReport());

            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<NewsletterDbContext>(o => o.UseInMemoryDatabase(dbName));
            services.AddScoped<ISteamNewGamesService>(_ => _service.Object);
            _provider = services.BuildServiceProvider();
        }

        public void Dispose() => _provider.Dispose();

        // ---------- Ayudas ----------

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _now;

            public FixedTimeProvider(DateTime utcNow) => _now = new DateTimeOffset(utcNow);

            public override DateTimeOffset GetUtcNow() => _now;
        }

        private sealed class StaticOptionsMonitor : IOptionsMonitor<SteamNewGamesOptions>
        {
            public StaticOptionsMonitor(SteamNewGamesOptions value) => CurrentValue = value;

            public SteamNewGamesOptions CurrentValue { get; }

            public SteamNewGamesOptions Get(string? name) => CurrentValue;

            public IDisposable? OnChange(Action<SteamNewGamesOptions, string?> listener) => null;
        }

        private sealed class InvalidOptionsMonitor : IOptionsMonitor<SteamNewGamesOptions>
        {
            public SteamNewGamesOptions CurrentValue => throw new OptionsValidationException(
                string.Empty, typeof(SteamNewGamesOptions), new[] { "IntervalDays debe estar entre 1 y 365" });

            public SteamNewGamesOptions Get(string? name) => CurrentValue;

            public IDisposable? OnChange(Action<SteamNewGamesOptions, string?> listener) => null;
        }

        private SteamNewGamesWorker CreateWorker() => new(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            new StaticOptionsMonitor(_options),
            new FixedTimeProvider(Now),
            NullLogger<SteamNewGamesWorker>.Instance);

        private async Task GuardarUltimaCorrida(DateTime lastRunUtc)
        {
            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NewsletterDbContext>();
            db.SteamSyncStates.Add(new SteamSyncState { Id = 1, LastRunUtc = lastRunUtc });
            await db.SaveChangesAsync();
        }

        private void VerificarEjecuciones(Times times) =>
            _service.Verify(s => s.RunAsync(It.IsAny<CancellationToken>()), times);

        // ---------- Pruebas ----------

        [Fact]
        public async Task RunIfDueAsync_CuandoNuncaCorrio_EjecutaLaBusqueda()
        {
            var ran = await CreateWorker().RunIfDueAsync(CancellationToken.None);

            Assert.True(ran);
            VerificarEjecuciones(Times.Once());
        }

        [Fact]
        public async Task RunIfDueAsync_CuandoCorrioHaceMenosDeIntervalDays_NoEjecuta()
        {
            await GuardarUltimaCorrida(Now.AddDays(-3));

            var ran = await CreateWorker().RunIfDueAsync(CancellationToken.None);

            Assert.False(ran);
            VerificarEjecuciones(Times.Never());
        }

        [Fact]
        public async Task RunIfDueAsync_CuandoPasaronIntervalDays_Ejecuta()
        {
            await GuardarUltimaCorrida(Now.AddDays(-7));

            var ran = await CreateWorker().RunIfDueAsync(CancellationToken.None);

            Assert.True(ran);
            VerificarEjecuciones(Times.Once());
        }

        [Fact]
        public async Task RunIfDueAsync_ConIntervalDaysEnUno_EjecutaAlDiaSiguiente()
        {
            _options.IntervalDays = 1;
            await GuardarUltimaCorrida(Now.AddHours(-25));

            var ran = await CreateWorker().RunIfDueAsync(CancellationToken.None);

            Assert.True(ran);
        }

        [Fact]
        public async Task RunIfDueAsync_CuandoEstaDesactivado_NoEjecuta()
        {
            _options.Enabled = false;

            var ran = await CreateWorker().RunIfDueAsync(CancellationToken.None);

            Assert.False(ran);
            VerificarEjecuciones(Times.Never());
        }

        [Fact]
        public async Task RunIfDueAsync_SinApiKey_NoEjecuta()
        {
            _options.ApiKey = "";

            var ran = await CreateWorker().RunIfDueAsync(CancellationToken.None);

            Assert.False(ran);
            VerificarEjecuciones(Times.Never());
        }

        [Fact]
        public async Task RunIfDueAsync_CuandoLaBusquedaFalla_NoPropagaElError()
        {
            _service.Setup(s => s.RunAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Steam caído"));

            var ran = await CreateWorker().RunIfDueAsync(CancellationToken.None);

            Assert.False(ran);
        }

        [Fact]
        public async Task RunIfDueAsync_ConConfiguracionInvalida_NoEjecutaNiPropagaElError()
        {
            var worker = new SteamNewGamesWorker(
                _provider.GetRequiredService<IServiceScopeFactory>(),
                new InvalidOptionsMonitor(),
                new FixedTimeProvider(Now),
                NullLogger<SteamNewGamesWorker>.Instance);

            var ran = await worker.RunIfDueAsync(CancellationToken.None);

            Assert.False(ran);
            VerificarEjecuciones(Times.Never());
        }
    }
}
