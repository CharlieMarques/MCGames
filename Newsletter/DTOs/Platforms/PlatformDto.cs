using System.ComponentModel.DataAnnotations;

namespace Newsletter.DTOs.Platforms
{
    public class PlatformDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string UrlLogo { get; set; } = string.Empty;
        public bool State { get; set; }
    }
}
