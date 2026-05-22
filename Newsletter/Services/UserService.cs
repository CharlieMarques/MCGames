using Microsoft.AspNetCore.Identity;
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
        public UserService(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration, ILibraryService libraryService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _libraryService = libraryService;
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
