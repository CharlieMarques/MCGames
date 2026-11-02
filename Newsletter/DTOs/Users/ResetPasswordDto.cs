using System.ComponentModel.DataAnnotations;

namespace Newsletter.DTOs.Users
{
    public class ResetPasswordDto
    {
        [Required] public string UserId { get; set; } = string.Empty;
        [Required] public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debes confirmar la nueva contraseña.")]
        [Compare("NewPassword", ErrorMessage = "La nueva contraseña y la confirmación no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
