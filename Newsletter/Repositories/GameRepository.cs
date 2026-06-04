using Microsoft.EntityFrameworkCore;
using Newsletter.Data;
using Newsletter.Models;

namespace Newsletter.Repositories
{
    public class GameRepository : IGameRepository
    {
        private readonly NewsletterDbContext _context;
        public GameRepository(NewsletterDbContext context)
        {
           _context = context;
        }
        public async Task AddAsync(Game game)
        {
            await _context.Games.AddAsync(game);
        }

        public void Delete(Game game)
        {
            _context.Games.Remove(game);
        }

        public async Task<Game?>GetByIdAsync(Guid id)
        {
            return await _context.Games.FindAsync(id);
        }

        public async Task<IEnumerable<Game>> GetMultiPlatformGamesAsync()
        {
            return await _context.Games
        .Include(g => g.EpicData) 
        .Where(g => g.SteamAppId != null && g.EpicData != null)
        .ToListAsync();
        }

        public IQueryable<Game> GetQueryable()
        {
            return _context.Games.AsQueryable();
        }

        public async Task SaveChangesAsync()
        {
           await _context.SaveChangesAsync();
        }

        public void Update(Game game)
        {
            _context.Update(game);
        }
    }
}
