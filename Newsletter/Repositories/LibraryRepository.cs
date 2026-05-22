using Microsoft.EntityFrameworkCore;
using Newsletter.Data;
using Newsletter.Models;

namespace Newsletter.Repositories
{
    public class LibraryRepository : ILibraryRepository
    {
        private readonly NewsletterDbContext _context;

        public LibraryRepository( NewsletterDbContext context)
        {
            _context = context;
        }
        public IQueryable<Game> GetGamesInLibraryAsync()
        {
            return _context.Games.AsQueryable();
        }

        public IQueryable<Library> GetQueryableLibraries()
        {
            return _context.Libraries.AsQueryable();
        }

        public async Task AddLibraryAsync(Library Library)
        {
             await _context.Libraries.AddAsync(Library);
        }

        public void Delete(Library library)
        {
            _context.Libraries.Remove(library);
        }

        public void UpdateGame(Library library)
        {
            _context.Libraries.Update(library);
        }

        public async Task<Library?> GetLibraryByIdAsync(Guid id)
        {
            return await _context.FindAsync<Library>(id);
        }

        public async Task SaveChangesAsync()
        {
           await _context.SaveChangesAsync();
        }

        public async Task AddGameInLibraryAsync(GameLibrary game)
        {
           await _context.GameLibraries.AddAsync(game);
        }

        public void DeleteGameInLibrary(GameLibrary game)
        {
            _context.GameLibraries.Remove(game);
        }
        public async Task<GameLibrary?> GetGameLibraryByIdAsync(Guid libraryId, Guid gameId)
        {
            return await _context.GameLibraries.FindAsync(libraryId,gameId);
        }
        public IQueryable<GameLibrary> GetQueryableGameLibraries()
        {
            return _context.GameLibraries.AsQueryable();
        }

        public void HideGameInLibrary(GameLibrary game)
        {
            _context.GameLibraries.Update(game);
        }

        public void Update(Library library)
        {
            throw new NotImplementedException();
        }

        public void UpdateGameLibrary(GameLibrary gameLibrary)
        {
            _context.GameLibraries.Update(gameLibrary);
        }

        public async Task<Library?> GetLibraryByUserAsync(string userId)
        {
           return await _context.Libraries.FirstOrDefaultAsync(l => l.UserId == userId);
        }
    }
}
