using System.ComponentModel.DataAnnotations;

namespace Newsletter.Integrations.Steam
{
    public class SteamNewGamesOptions
    {
        public const string SectionName = "SteamNewGames";

        public bool Enabled { get; set; } = true;

        [Range(1, 365)]
        public int IntervalDays { get; set; } = 7;

        [Range(1, 5000)]
        public int MaxPerRun { get; set; } = 1000;

        [Range(0, 60000)]
        public int RequestDelayMs { get; set; } = 1600;

        public string ApiKey { get; set; } = string.Empty;
    }
}
