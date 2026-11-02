using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Newsletter.Integrations.Steam
{
    public class SteamStoreClient : ISteamStoreClient
    {
        private const int PageSize = 50000;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _http;
        private readonly SteamNewGamesOptions _options;

        public SteamStoreClient(HttpClient http, IOptions<SteamNewGamesOptions> options)
        {
            _http = http;
            _options = options.Value;
        }

        public async IAsyncEnumerable<StoreApp> GetModifiedAppsAsync(
            DateTime? modifiedSinceUtc,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException("Falta SteamNewGames:ApiKey en la configuración.");
            }

            int lastAppId = 0;
            bool haveMore;
            do
            {
                var url = "https://api.steampowered.com/IStoreService/GetAppList/v1/"
                    + $"?key={Uri.EscapeDataString(_options.ApiKey)}&max_results={PageSize}";
                if (lastAppId > 0)
                {
                    url += $"&last_appid={lastAppId}";
                }
                if (modifiedSinceUtc.HasValue)
                {
                    var utc = DateTime.SpecifyKind(modifiedSinceUtc.Value, DateTimeKind.Utc);
                    url += $"&if_modified_since={new DateTimeOffset(utc).ToUnixTimeSeconds()}";
                }

                var json = await GetStringAsync(url, cancellationToken);
                var payload = JsonSerializer.Deserialize<StoreAppListResponse>(json, JsonOptions)?.Response;

                if (payload?.Apps != null)
                {
                    foreach (var app in payload.Apps)
                    {
                        yield return app;
                    }
                }

                haveMore = payload?.HaveMoreResults ?? false;
                if (haveMore && payload!.LastAppId <= lastAppId)
                {
                    break; // Steam no avanzó: cortamos para no entrar en un bucle infinito
                }
                lastAppId = payload?.LastAppId ?? 0;
            }
            while (haveMore);
        }

        public async Task<SteamGameData?> GetAppDetailsAsync(int appId, CancellationToken cancellationToken = default)
        {
            var url = $"https://store.steampowered.com/api/appdetails?appids={appId}&cc=ar&l=spanish";
            var json = await GetStringAsync(url, cancellationToken);

            var result = JsonSerializer.Deserialize<Dictionary<string, SteamGameWrapper>>(json, JsonOptions);
            if (result == null
                || !result.TryGetValue(appId.ToString(), out var wrapper)
                || wrapper == null
                || !wrapper.Success
                || wrapper.Data.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return JsonSerializer.Deserialize<SteamGameData>(wrapper.Data.GetRawText(), JsonOptions);
        }

        private async Task<string> GetStringAsync(string url, CancellationToken cancellationToken)
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new SteamRateLimitException();
            }
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
    }
}
