namespace Newsletter.Integrations.Steam
{
    public class SteamRateLimitException : Exception
    {
        public SteamRateLimitException()
            : base("Steam respondió 429 (demasiadas solicitudes).") { }
    }
}
