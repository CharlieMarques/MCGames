using Newsletter.Models;

namespace Newsletter.Repositories
{
    public interface IGenreRepository
    {
        IQueryable<Genre> GetQueryable();
        Task<Genre?> GetByIdAsync(int id);
        Task AddAsync(Genre genre);
        void Update(Genre genre);
        void Delete(Genre genre);
        Task<bool> GenreExists(string description);
        Task SaveChangesAsync();
        Task<bool> AllExistAsync(List<int> ids);
    }
}
