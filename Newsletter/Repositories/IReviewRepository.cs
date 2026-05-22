using Newsletter.Models;

namespace Newsletter.Repositories
{
    public interface IReviewRepository
    {
        IQueryable<Review> GetQueryable();
        Task<Review?> GetByIdAsync(Guid id);
        Task AddAsync(Review review);
        void Update(Review review);
        void Delete(Review review);
        Task SaveChangesAsync();
        Task<bool> ReviewExists(string userId, Guid gameId);
    }
}
