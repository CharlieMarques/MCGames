namespace Newsletter.Integrations.Steam
{
    public interface ISteamNewGamesService
    {
        Task<SteamSyncReport> RunAsync(CancellationToken cancellationToken = default);
    }
}
