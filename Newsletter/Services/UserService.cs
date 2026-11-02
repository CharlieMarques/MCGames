using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using Newsletter.DTOs.Users;
using Newsletter.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Newsletter.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILibraryService _libraryService;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserService> _logger;
        public UserService(UserManager<User> userManager, RoleManager<IdentityRole> roleManager,
            IConfiguration configuration, ILibraryService libraryService, IEmailService emailService,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _libraryService = libraryService;
            _emailService = emailService;
            _logger = logger;
        }
        public async Task<(bool success, IEnumerable<string> Errors)> AssignRoleAsycn(UserRoleDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if(user ==null)
            {
                return (false, new[]{"Usuario no encontrado"});
            }
            if (!await _roleManager.RoleExistsAsync(dto.Role))
            {
                return (false, new[] { "Rol inexistente" });
            }
            var result = await _userManager.AddToRoleAsync(user, dto.Role);
            if (result.Succeeded)
            {
                return (true, Array.Empty<string>());
            }
            return (false, result.Errors.Select(e =>  e.Description));
        }

        public async Task<(bool success, string errorMessage)> ChangePasswordAsync(string userId, UserChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (false, "Usuario no encontrado");

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
                    return (false, "Contraseña actual incorrecta");
                return (false, string.Join(" ", result.Errors.Select(e => e.Description)));
            }

            await NotifyPasswordChangedAsync(user);
            return (true, string.Empty);
        }

        public async Task<(bool success, string Token, string ErrorMessage, bool EmailNotConfirmed)> LoginAsync(UserLoginDto dto)
        {
            var user = await _userManager.FindByNameAsync(dto.UserName);
            if(user == null)
            {
                return (false, string.Empty, "Usuario o contraseña incorrectos", false);
            }
            var result = await _userManager.CheckPasswordAsync(user, dto.Password);
            if(!result)
            {
                return (false, string.Empty, "Usuario o contraseña incorrectos", false);
            }
            // Se revisa después de la contraseña: así no se revela si una cuenta existe o está confirmada.
            if (!user.EmailConfirmed)
            {
                return (false, string.Empty, "Tenés que confirmar tu email antes de iniciar sesión. Revisá tu correo o pedí un nuevo enlace.", true);
            }
            var token = await GenerateTokenJWT(user);
            return (true, token, string.Empty, false);
        }

        public async Task<(bool success, IEnumerable<string> Errors)> RegisterAsync(UserRegisterDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
            {
                return (false, new[] { "Las contraseñas no coinciden" });
            }
            var newUser = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                LogoUrl = "default-profile.png"
            };
            var result = await _userManager.CreateAsync(newUser, dto.Password);
            if (!result.Succeeded)
            {
                return (false, result.Errors.Select(e => e.Description));
            }

            // Primero lo que deja la cuenta completa (rol y biblioteca); el email va al final.
            var roleResult = await _userManager.AddToRoleAsync(newUser, "Usuario");
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Usuario {UserName} creado pero no se le pudo asignar el rol Usuario: {Errors}",
                    newUser.UserName, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }

            var libraryResult = await _libraryService.CreateLibraryAsync(newUser.Id.ToString());
            if (!libraryResult.success)
            {
                _logger.LogWarning("Usuario {UserName} creado pero falló su biblioteca: {Error}",
                    newUser.UserName, libraryResult.ErrorMessage);
            }

            // Si el email falla, el registro igual es válido: el usuario puede pedir el reenvío.
            try
            {
                await SendConfirmationEmailAsync(newUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el email de confirmación a {UserName}; puede pedir el reenvío.",
                    newUser.UserName);
            }

            return (true, Array.Empty<string>());
        }

        public async Task<(bool success, string message)> ConfirmEmailAsync(string userId, string token)
        {           
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return (false, "Faltan parámetros de confirmación.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, "Usuario no encontrado.");
            }

            try
            {
                var decodedTokenBytes = WebEncoders.Base64UrlDecode(token);
                var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);

                var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

                if (result.Succeeded)
                {
                    return (true, "Email confirmado exitosamente. Ya puedes iniciar sesión.");
                }

                return (false, "El token es inválido o ha expirado.");
            }
            catch (FormatException)
            {
                return (false, "El token de confirmación está corrupto.");
            }
        }

        public async Task ResendConfirmationEmailAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null || user.EmailConfirmed)
                {
                    // Mismo resultado para todos: no se revela si el email existe o ya está confirmado.
                    return;
                }
                await SendConfirmationEmailAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo reenviar el email de confirmación.");
            }
        }

        public async Task ForgotPasswordAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null || !user.EmailConfirmed)
                {
                    // Mismo resultado para todos: no se revela si el email existe.
                    return;
                }
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var link = BuildFrontendLink("FrontendUrl:ResetPasswordUrl", user, token);
                await _emailService.SendPasswordResetAsync(user.Email!, link);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el email de recuperación de contraseña.");
            }
        }

        public async Task<(bool success, IEnumerable<string> Errors, bool InvalidLink)> ResetPasswordAsync(ResetPasswordDto dto)
        {
            const string invalidLinkMessage = "El enlace no es válido o venció. Pedí uno nuevo.";

            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
                return (false, new[] { invalidLinkMessage }, true);

            string token;
            try
            {
                token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.Token));
            }
            catch (FormatException)
            {
                return (false, new[] { invalidLinkMessage }, true);
            }

            var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
                    return (false, new[] { invalidLinkMessage }, true);
                return (false, result.Errors.Select(e => e.Description), false);
            }

            await NotifyPasswordChangedAsync(user);
            return (true, Array.Empty<string>(), false);
        }

        private async Task NotifyPasswordChangedAsync(User user)
        {
            if (string.IsNullOrEmpty(user.Email))
                return;
            try
            {
                await _emailService.SendPasswordChangedNoticeAsync(user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el aviso de contraseña cambiada a {UserName}.", user.UserName);
            }
        }

        private async Task SendConfirmationEmailAsync(User user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = BuildFrontendLink("FrontendUrl:Url", user, token);
            await _emailService.SendEmailConfirmationAsync(user.Email!, confirmationLink);
        }

        private string BuildFrontendLink(string configKey, User user, string token)
        {
            var baseUrl = _configuration[configKey];
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException($"Falta {configKey} en la configuración.");

            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var separator = baseUrl.Contains('?') ? '&' : '?';
            return $"{baseUrl}{separator}userId={Uri.EscapeDataString(user.Id)}&token={encodedToken}";
        }

        private async Task<string>GenerateTokenJWT(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier,user.Id),
                new Claim(JwtRegisteredClaimNames.Sub,user.UserName),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            foreach(var rol in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, rol));
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(1),
                    signingCredentials : creds
                );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
