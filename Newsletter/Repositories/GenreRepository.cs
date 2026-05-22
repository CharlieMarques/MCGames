using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Newsletter.Data;
using Newsletter.Models;

namespace Newsletter.Repositories
{
    public class GenreRepository : IGenreRepository
    {
        private readonly NewsletterDbContext _context;
        public GenreRepository(NewsletterDbContext context)
        {
            _context = context;
        }
        public IQueryable<Genre> GetQueryable()
        {
            return _context.Genres.AsQueryable();
        }
        public async Task<Genre?> GetByIdAsync(int id)
        {
            return await _context.Genres.FindAsync(id);
        }
        public async Task AddAsync(Genre genre)
        {
            await _context.Genres.AddAsync(genre);
        }
        public void Update(Genre genre)
        {
            _context.Genres.Update(genre);
        }
        public void Delete(Genre genre)
        {
            _context.Genres.Remove(genre);           
        }
        public async Task<bool> GenreExists(string description)
        {
            return await _context.Genres.AnyAsync(g => g.Description.ToLower() == description.ToLower());
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<bool> AllExistAsync(List<int> ids)
        {
            if(ids ==null || !ids.Any()) 
                return true;
            var uniqueIds = ids.Distinct().ToList();
            var existingCount = await _context.Genres
                .Where(g => uniqueIds.Contains(g.Id))
                .CountAsync();
            return existingCount  == uniqueIds.Count();
        }

    }
}
