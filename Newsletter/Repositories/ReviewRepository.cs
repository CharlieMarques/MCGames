using Microsoft.EntityFrameworkCore;
using Newsletter.Data;
using Newsletter.Models;

namespace Newsletter.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly NewsletterDbContext _context;

        public ReviewRepository(NewsletterDbContext context)
        {
            _context = context;
        }
        public IQueryable<Review> GetQueryable()
        {
            return _context.Reviews.AsQueryable();
        }
        public async Task<Review?> GetByIdAsync(Guid id)
        {
            return await _context.Reviews.FindAsync(id);
        }
        public async Task AddAsync(Review review)
        {
            await _context.Reviews.AddAsync(review);
        }
        public void Update(Review review)
        {
            _context.Reviews.Update(review);
        }
        public void Delete(Review review)
        {
            _context.Reviews.Remove(review);
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<bool> ReviewExists(string userId, Guid gameId)
        {
            return await _context.Reviews.AnyAsync(r => r.UserId == userId && r.GameId == gameId);
        }
    }
}
