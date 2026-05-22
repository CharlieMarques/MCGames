using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Newsletter.Models
{
    public class User :IdentityUser
    {
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        public DateTime DateBirth { get; set; }
        public string LogoUrl { get; set; } = string.Empty;
        public List<Review>? Reviews { get; set; }
        public GameLibrary? GameLibrary { get; set; }
    }
}
