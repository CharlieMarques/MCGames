using System.ComponentModel.DataAnnotations;

namespace Newsletter.DTOs.Games
{
    public class GameDto
    {
        public Guid? Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;       
        [Required]      
        public string ShortDescription { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public string GameCoverUrl { get; set; } = string.Empty;
        public bool State { get; set; }
    }
}
