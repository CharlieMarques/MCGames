using System.ComponentModel.DataAnnotations;

namespace Newsletter.Models
{
    public class Genre
    {
        public int Id { get; set; }
        [StringLength(50)]
        public string Description { get; set; } = string.Empty;
        public bool State { get; set; }
        public List<GameGenre>? GameGenre { get; set; }
    }
}
