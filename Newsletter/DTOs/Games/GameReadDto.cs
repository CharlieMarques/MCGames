using Newsletter.DTOs.Category;
using Newsletter.DTOs.Genres;
using Newsletter.DTOs.Platforms;
using Newsletter.Models;

namespace Newsletter.DTOs.Games
{
    public class GameReadDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public DateTime? ReleaseDate {  get; set; }
        public string GameCoverUrl { get; set; } = string.Empty;
        public bool State {  get; set; }
        public decimal Price { get; set; }
        public bool OnOffer { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal FinalPrice { get; set; }
        public int? SteamAppId { get; set; }

        public string? EpicStoreId { get; set; }
        public decimal? EpicPrice { get; set; }
        public decimal? EpicFinalPrice { get; set; }
        public int? EpicDiscountPercentage { get; set; }
        public bool? EpicOnOffer { get; set; }
        public string? PageSlug { get; set; }

        public List<CategoryReadDto>? Categories { get; set; }
        public List<GenreDto>? Genres { get; set; }
        public List<PlatformDto>? Platforms {  get; set; }

        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
    }
}
