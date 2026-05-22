using System.ComponentModel.DataAnnotations;

namespace Newsletter.DTOs.Users
{
    public class UserRegisterDto
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        public string UserName { get; set; }
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string Email { get;set; }
        [Required]
        public string Password { get; set; }
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; }
    }
}
