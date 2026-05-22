using Newsletter.Models;

namespace Newsletter.Repositories
{
    public interface IGameRepository
    {
        IQueryable<Game> GetQueryable();
        Task<Game?> GetByIdAsync(Guid id);
        Task AddAsync(Game game);
        void Update(Game game);
        void Delete(Game game);
        Task SaveChangesAsync();
    }
}
