namespace Newsletter.Models
{
    public class GamePlatform
    {
        public Guid GameId { get; set; }
        public Game? Game { get; set; }
        public int PlatformId { get; set; }
        public Platform? Platform { get; set; }
        public bool State { get; set; }
        public DateTime AddedDate { get; set; }
    }
}
