using System.ComponentModel.DataAnnotations;

namespace Newsletter.DTOs.Users
{
    public class UserRoleDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string Role { get; set; }
    }
}