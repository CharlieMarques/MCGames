using System.Net;
using System.Text;
using Newsletter.Integrations.Steam;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Newsletter.UnitTests.Integrations.Steam
{
    public class SteamStoreClientTests
    {
        private const string ApiKey = "clave-secreta-de-prueba";

        private const string JuegoJson = """{"123":{"success":true,"data":{"name":"Juego","short_description":"Desc","header_image":"http://x/y.jpg","is_free":false,"type":"game","release_date":{"coming_soon":false,"date":"12 oct 2026"},"price_overview":{"initial":100000,"final":50000,"discount_percent":50},"genres":[{"id":"1","description":"Acción"}],"platforms":{"windows":true,"mac":false,"linux":true},"categories":[{"id":2,"description":"Un jugador"}]}}}""";

        private const string ListaVaciaJson = """{"response":{"apps":[],"have_more_results":false,"last_appid":0}}""";

        // ---------- Ayudas ----------

        private sealed class FakeHandler : HttpMessageHandler
        {
            private readonly Queue<(HttpStatusCode Status, string Body)> _responses = new();

            public List<Uri> Requests { get; } = new();

            public FakeHandler(params (HttpStatusCode Status, string Body)[] responses)
            {
                foreach (var response in responses)
                {
                    _responses.Enqueue(response);
                }
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request.RequestUri!);
                var (status, body) = _responses.Dequeue();
                return Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                });
            }
        }

        private static (HttpStatusCode, string) Ok(string body) => (HttpStatusCode.OK, body);

        private static SteamStoreClient CreateClient(FakeHandler handler, string apiKey = ApiKey)
        {
            var options = MsOptions.Create(new SteamNewGamesOptions { ApiKey = apiKey });
            return new SteamStoreClient(new HttpClient(handler), options);
        }

        private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> source)
        {
            var list = new List<T>();
            await foreach (var item in source)
            {
                list.Add(item);
            }
            return list;
        }

        // ---------- GetModifiedAppsAsync ----------

        [Fact]
        public async Task GetModifiedAppsAsync_CuandoHayMasResultados_PideLaPaginaSiguienteYJuntaTodo()
        {
            var handler = new FakeHandler(
                Ok("""{"response":{"apps":[{"appid":10,"name":"A","last_modified":1},{"appid":20,"name":"B","last_modified":2}],"have_more_results":true,"last_appid":20}}"""),
                Ok("""{"response":{"apps":[{"appid":30,"name":"C","last_modified":3}],"have_more_results":false,"last_appid":30}}"""));
            var client = CreateClient(handler);

            var apps = await ToListAsync(client.GetModifiedAppsAsync(null));

            Assert.Equal(new[] { 10, 20, 30 }, apps.Select(a => a.AppId));
            Assert.Equal(2, handler.Requests.Count);
            Assert.Contains("key=" + ApiKey, handler.Requests[0].Query);
            Assert.DoesNotContain("last_appid", handler.Requests[0].Query);
            Assert.Contains("last_appid=20", handler.Requests[1].Query);
        }

        [Fact]
        public async Task GetModifiedAppsAsync_ConFechaDeCorte_EnviaIfModifiedSinceEnSegundosUnix()
        {
            var handler = new FakeHandler(Ok(ListaVaciaJson));
            var client = CreateClient(handler);
            var desde = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

            await ToListAsync(client.GetModifiedAppsAsync(desde));

            Assert.Contains("if_modified_since=1790812800", handler.Requests[0].Query);
        }

        [Fact]
        public async Task GetModifiedAppsAsync_CuandoLaFechaNoTieneKind_LaTrataComoUtc()
        {
            // EF devuelve las columnas datetime2 con Kind = Unspecified.
            var handler = new FakeHandler(Ok(ListaVaciaJson));
            var client = CreateClient(handler);
            var desde = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Unspecified);

            await ToListAsync(client.GetModifiedAppsAsync(desde));

            Assert.Contains("if_modified_since=1790812800", handler.Requests[0].Query);
        }

        [Fact]
        public async Task GetModifiedAppsAsync_SinFechaDeCorte_NoEnviaIfModifiedSince()
        {
            var handler = new FakeHandler(Ok(ListaVaciaJson));
            var client = CreateClient(handler);

            await ToListAsync(client.GetModifiedAppsAsync(null));

            Assert.DoesNotContain("if_modified_since", handler.Requests[0].Query);
        }

        [Fact]
        public async Task GetModifiedAppsAsync_CuandoLaRespuestaVieneSinApps_DevuelveListaVacia()
        {
            var handler = new FakeHandler(Ok("""{"response":{}}"""));
            var client = CreateClient(handler);

            var apps = await ToListAsync(client.GetModifiedAppsAsync(null));

            Assert.Empty(apps);
            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task GetModifiedAppsAsync_CuandoSteamResponde429_LanzaSteamRateLimitException()
        {
            var handler = new FakeHandler((HttpStatusCode.TooManyRequests, ""));
            var client = CreateClient(handler);

            await Assert.ThrowsAsync<SteamRateLimitException>(() =>
                ToListAsync(client.GetModifiedAppsAsync(null)));
        }

        [Fact]
        public async Task GetModifiedAppsAsync_SinApiKey_LanzaInvalidOperationExceptionYNoLlamaASteam()
        {
            var handler = new FakeHandler();
            var client = CreateClient(handler, apiKey: "");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ToListAsync(client.GetModifiedAppsAsync(null)));

            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task GetModifiedAppsAsync_CuandoSteamResponde500_LanzaHttpRequestExceptionSinMostrarLaKey()
        {
            var handler = new FakeHandler((HttpStatusCode.InternalServerError, ""));
            var client = CreateClient(handler);

            var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
                ToListAsync(client.GetModifiedAppsAsync(null)));

            Assert.DoesNotContain(ApiKey, ex.Message);
        }

        // ---------- GetAppDetailsAsync ----------

        [Fact]
        public async Task GetAppDetailsAsync_CuandoSuccessEsTrue_MapeaLosDatosDelJuego()
        {
            var handler = new FakeHandler(Ok(JuegoJson));
            var client = CreateClient(handler);

            var game = await client.GetAppDetailsAsync(123);

            Assert.NotNull(game);
            Assert.Equal("Juego", game.Name);
            Assert.Equal("Desc", game.ShortDescription);
            Assert.Equal("http://x/y.jpg", game.HeaderImage);
            Assert.Equal("game", game.Type);
            Assert.False(game.IsFree);
            Assert.False(game.ReleaseDate.ComingSoon);
            Assert.Equal("12 oct 2026", game.ReleaseDate.Date);
            Assert.Equal(100000, game.PriceOverview.Initial);
            Assert.Equal(50000, game.PriceOverview.Final);
            Assert.Equal(50, game.PriceOverview.DiscountPercent);
            Assert.Equal("Acción", Assert.Single(game.Genres).Description);
            var category = Assert.Single(game.Categories);
            Assert.Equal(2, category.Id);
            Assert.Equal("Un jugador", category.Description);
            Assert.True(game.Platforms.Windows);
            Assert.False(game.Platforms.Mac);
            Assert.True(game.Platforms.Linux);
        }

        [Fact]
        public async Task GetAppDetailsAsync_CuandoElJuegoEsProximo_MarcaComingSoon()
        {
            var json = JuegoJson.Replace(
                "\"coming_soon\":false,\"date\":\"12 oct 2026\"",
                "\"coming_soon\":true,\"date\":\"Próximamente\"");
            var client = CreateClient(new FakeHandler(Ok(json)));

            var game = await client.GetAppDetailsAsync(123);

            Assert.NotNull(game);
            Assert.True(game.ReleaseDate.ComingSoon);
        }

        [Fact]
        public async Task GetAppDetailsAsync_PideElJuegoEnPesosArgentinosYEnEspanol()
        {
            var handler = new FakeHandler(Ok(JuegoJson));
            var client = CreateClient(handler);

            await client.GetAppDetailsAsync(123);

            var query = handler.Requests[0].Query;
            Assert.Contains("appids=123", query);
            Assert.Contains("cc=ar", query);
            Assert.Contains("l=spanish", query);
        }

        [Fact]
        public async Task GetAppDetailsAsync_CuandoSuccessEsFalse_DevuelveNull()
        {
            var client = CreateClient(new FakeHandler(Ok("""{"123":{"success":false}}""")));

            var game = await client.GetAppDetailsAsync(123);

            Assert.Null(game);
        }

        [Fact]
        public async Task GetAppDetailsAsync_CuandoDataEsUnArregloVacio_DevuelveNull()
        {
            // Steam a veces manda "data": [] en lugar de un objeto.
            var client = CreateClient(new FakeHandler(Ok("""{"123":{"success":true,"data":[]}}""")));

            var game = await client.GetAppDetailsAsync(123);

            Assert.Null(game);
        }

        [Fact]
        public async Task GetAppDetailsAsync_CuandoSteamResponde429_LanzaSteamRateLimitException()
        {
            var client = CreateClient(new FakeHandler((HttpStatusCode.TooManyRequests, "")));

            await Assert.ThrowsAsync<SteamRateLimitException>(() => client.GetAppDetailsAsync(123));
        }
    }
}
