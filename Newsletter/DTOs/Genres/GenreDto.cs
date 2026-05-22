using System.ComponentModel.DataAnnotations;

namespace Newsletter.DTOs.Genres
{
    public class GenreDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool State { get; set; }
    }
}
