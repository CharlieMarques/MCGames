using Newsletter.DTOs.Genres;
using Newsletter.DTOs.Platforms;

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

        public List<GenreDto>? Genres { get; set; }
        public List<PlatformDto>? Platforms {  get; set; }

        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
    }
}
