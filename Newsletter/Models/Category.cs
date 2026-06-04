using System.ComponentModel.DataAnnotations;

namespace Newsletter.Models
{
    public class Category
    {
        public int Id { get; set; }
        [StringLength(50)]
        public string Description { get; set; } = string.Empty;
        public List<GameCategory>? GameCategories { get; set; }
    }
}
