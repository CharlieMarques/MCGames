using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using Newsletter.DTOs.Users;
using Newsletter.Models;
using Newsletter.Repositories;
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
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        public UserService(UserManager<User> userManager, RoleManager<IdentityRole> roleManager,
            IConfiguration configuration, ILibraryService libraryService, IUserRepository userRepository, IPasswordHasher<User> passwordHasher, IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _libraryService = libraryService;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
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
            var user = await _userRepository.GetbyIdAsync(userId);
            if (user == null)
                return (false, "Usuario no encontrado");

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
            if (verificationResult == PasswordVerificationResult.Failed)
                return (false, "Contraseña actual incorrecta");
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            var updateResult = await _userRepository.UpdateAsync(user);
            if(!updateResult)
            {
                return (false, "Error al actualizar la contraseña");
            }
            return (true, string.Empty);
        }

        public async Task<(bool success, string Token, string ErrorMessage)> LoginAsync(UserLoginDto dto)
        {
            var user = await _userManager.FindByNameAsync(dto.UserName);
            if(user == null)
            {
                return (false, string.Empty, "Usuario o contraseña incorrectos");
            }
            var result = await _userManager.CheckPasswordAsync(user, dto.Password);
            if(!result)
            {
                return (false, string.Empty, "Usuario o contraseña incorrectos");
            }
            var token = await GenerateTokenJWT(user);
            return (true, token, string.Empty);
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

            if (result.Succeeded)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var  frontendUrl = _configuration["FrontendUrl:Url"];
                var confirmationLink = $"{frontendUrl}/ConfirmEmail?userId={newUser.Id}&token={encodedToken}";
                await _emailService.SendEmailConfirmationAsync(newUser.Email, confirmationLink);
                await _userManager.AddToRoleAsync(newUser, "Usuario");
                var libraryResult = await _libraryService.CreateLibraryAsync(newUser.Id.ToString());
                if (!libraryResult.success)
                {
                    Console.WriteLine($"Alerta: Usuario {newUser.UserName} creado pero falló su biblioteca: {libraryResult.ErrorMessage}");
                }
                return (true, Array.Empty<string>());
            }
            var errors = result.Errors.Select(e => e.Description);
            return (false, errors);
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
