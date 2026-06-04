namespace Newsletter.Models
{
    public class GameCategory
    {
        public Guid GameId { get; set; }
        public int CategoryId { get; set; }
        public Game? Game { get; set; }
        public Category? Category { get; set; }
    }
}
