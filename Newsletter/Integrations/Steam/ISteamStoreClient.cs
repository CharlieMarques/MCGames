
namespace Newsletter.Integrations.Steam
{
    public interface ISteamStoreClient
    {
        IAsyncEnumerable<StoreApp> GetModifiedAppsAsync(DateTime? modifiedSinceUtc, CancellationToken cancellationToken = default);
        Task<SteamGameData?> GetAppDetailsAsync(int appId, CancellationToken cancellationToken = default);
    }
}
