using System.ComponentModel.DataAnnotations;

namespace Newsletter.DTOs.Reviews
{
    public class ReviewCreateDto
    {
        [Required]
        public Guid GameId { get; set; }
        public string UserId { get; set; } = string.Empty;
        [Required]
        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;
        [Required]
        [Range(1, 5, ErrorMessage ="El valor debe estar entre 1 y 5")]
        public int Rating { get; set; }
    }
}
