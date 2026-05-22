using System.ComponentModel.DataAnnotations;

namespace Newsletter.DTOs.Users
{
    public class UserLoginDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
