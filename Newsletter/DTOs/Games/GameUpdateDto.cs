using System.ComponentModel.DataAnnotations;

namespace Newsletter.DTOs.Games
{
    public class GameUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        [Required]
        [StringLength(1000)]
        public string ShortDescription { get; set; } = string.Empty;
        [Required]
        public DateTime ReleaseDate { get; set; }
        public string GameCoverUrl { get; set; } = string.Empty;
        [Range(0, 1000)]
        public decimal Price { get; set; }
        public bool OnOffer { get; set; }
        public bool State { get; set; }
    }
}
