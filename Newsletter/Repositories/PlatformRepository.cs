using Microsoft.EntityFrameworkCore;
using Newsletter.Data;
using Newsletter.Models;

namespace Newsletter.Repositories
{
    public class PlatformRepository : IPlatformRepository
    {
        public readonly NewsletterDbContext _context;
        public PlatformRepository(NewsletterDbContext context)
        {
            _context = context;
        }
        public IQueryable<Platform> GetQueryable()
        {
            return _context.Platforms.AsQueryable();
        }
        public async Task<Platform?> GetByIdAsync(int id)
        {
            return await _context.Platforms.FindAsync(id);
        }
        public async Task AddAsync(Platform platform)
        {
            await _context.Platforms.AddAsync(platform);
        }
        public void Update(Platform platform)
        {
            _context.Platforms.Update(platform);
        }
        public void Delete(Platform platform)
        {
            _context.Platforms.Remove(platform);
        }
        public async Task<bool> PlatformExists(string description)
        {
            return await _context.Platforms.AnyAsync(p => p.Description.ToLower() == description.ToLower());
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AllExistAsync(List<int> ids)
        {
            if (ids == null || !ids.Any())
                return true;
            var uniqueIds = ids.Distinct().ToList();
            var existingCount = await _context.Platforms
                .Where(g => uniqueIds.Contains(g.Id))
                .CountAsync();
            return existingCount == uniqueIds.Count();
        }
    }
}
