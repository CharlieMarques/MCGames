using Newsletter.DTOs.Users;

namespace Newsletter.Services
{
    public interface IUserService
    {
        Task<(bool success, IEnumerable<string> Errors)> RegisterAsync(UserRegisterDto dto);
        Task<(bool success, string message)> ConfirmEmailAsync(string userId, string token);
        Task<(bool success, string Token, string ErrorMessage)> LoginAsync(UserLoginDto dto);
        Task<(bool success, IEnumerable<string> Errors)> AssignRoleAsycn(UserRoleDto dto);
        Task<(bool success, string errorMessage)> ChangePasswordAsync(string userId, UserChangePasswordDto dto);
    }
}
