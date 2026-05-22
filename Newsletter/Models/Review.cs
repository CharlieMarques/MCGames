using System.ComponentModel.DataAnnotations;

namespace Newsletter.Models
{
    public class Review
    {
        public Guid Id { get; set; }
        public  Guid GameId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public bool State { get; set; } 
        [Required]
        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;
        [Range(1, 5)]
        public int Rating { get; set; }
        [Required]
        public DateTime ReviewDate { get; set; }
        [Timestamp]
        public byte[] RowVersion { get; set; }
        public User? User { get; set; }
        public Game? Game { get; set; }
    }
}
