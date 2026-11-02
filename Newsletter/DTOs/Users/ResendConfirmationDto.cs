using System.ComponentModel.DataAnnotations;

namespace Newsletter.DTOs.Users
{
    public class ResendConfirmationDto
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string Email { get; set; } = string.Empty;
    }
}
