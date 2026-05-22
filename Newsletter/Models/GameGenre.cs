namespace Newsletter.Models
{
    public class GameGenre
    {
        public Guid GameId { get; set; }
        public Game? Game { get; set; }
        public int GenreId { get; set; }
        public Genre? Genre { get; set; }
    }
}
