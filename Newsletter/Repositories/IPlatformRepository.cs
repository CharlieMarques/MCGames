using Newsletter.Models;

namespace Newsletter.Repositories
{
    public interface IPlatformRepository
    {
        IQueryable<Platform> GetQueryable();
        Task<Platform?> GetByIdAsync(int id);
        Task AddAsync(Platform platform);
        void Update(Platform platform);
        void Delete(Platform platform);
        Task<bool> PlatformExists(string description);
        Task<bool> AllExistAsync(List<int> ids);
        Task SaveChangesAsync();

    }
}
