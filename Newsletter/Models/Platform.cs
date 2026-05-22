using System.ComponentModel.DataAnnotations;

namespace Newsletter.Models
{
    public class Platform
    {
        public int Id { get; set; }
        [StringLength(100)]
        public string Description { get; set; } = string.Empty;
        [Url]
        public string UrlLogo { get; set; } = string.Empty;
        public bool State { get; set; }
        public List<GamePlatform>? GamePlatforms { get; set; }
    }
}
