using System.ComponentModel.DataAnnotations;

namespace Newsletter.Models
{
    public class Game
    {
        public Guid Id { get; set; }
        [StringLength(100)]
        [Required]
        public string Name { get; set; } = string.Empty;
        [StringLength(600)]
        [Required]
        public string ShortDescription { get; set; } = string.Empty;
        public DateTime? ReleaseDate { get; set; }
        public DateTime? LastPriceCheck {get; set; }
        public string GameCoverUrl { get; set; } = string.Empty;
        public bool State { get; set; }
        public decimal Price { get; set; }
        public bool OnOffer { get; set; } = false;
        public decimal DiscountPercentage { get; set; } = 0;
        public decimal FinalPrice { get; set; }
        public int? SteamAppId { get; set; }
        public List<GamePlatform>? GamePlatforms { get; set; }
        public List<GameGenre>? GameGenre { get; set; }
        public List<GameCategory>? GameCategories { get; set; }
        public List<Review>? Reviews { get; set; }
        public List<GameLibrary>? GameLibraries { get; set; }
        public virtual GameInEpic? EpicData { get; set; }
    }
}
