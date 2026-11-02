namespace Newsletter.Integrations.Steam
{
    public class SteamSyncReport
    {
        public DateTime? SinceUtc { get; set; }
        public int Candidates { get; set; }
        public int Inserted { get; set; }
        public int Linked { get; set; }
        public int SkippedComingSoon { get; set; }
        public int SkippedNotAGame { get; set; }
        public int SkippedNoData { get; set; }
        public int Failed { get; set; }
        public bool ReachedLimit { get; set; }
        public bool StoppedByErrors { get; set; }
        public bool DateAdvanced { get; set; }
    }
}
