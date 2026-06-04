namespace Newsletter.DTOs.Libraries
{
    public class ReadGamesLibraryDto
    {
        public Guid GameId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public string GameCoverUrl { get; set; } = string.Empty;
        public int? SteamAppId { get; set; }
    }
}
