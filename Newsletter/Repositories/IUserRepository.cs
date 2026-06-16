using Microsoft.AspNetCore.Identity;
using Newsletter.Models;
using System.Threading.Tasks;

namespace Newsletter.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetbyIdAsync(string id);
        Task<bool> UpdateAsync(User user);
        Task<IdentityResult> ChangePasswordAsync(User user, string currentPassword, string newPassword);
    }
}
