namespace Newsletter.Models
{
    public class GameLibrary
    {
        public Guid LibraryId { get; set; }
        public Library? Library { get; set; }

        public Guid GameId { get; set; }
        public Game? Game { get; set; }
        public bool State { get; set; }
        public DateTime AddedDate { get; set; }
    }
}
