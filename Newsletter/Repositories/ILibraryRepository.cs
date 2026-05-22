using Newsletter.Models;

namespace Newsletter.Repositories
{
    public interface ILibraryRepository
    {
        IQueryable<Library> GetQueryableLibraries();
        IQueryable<GameLibrary> GetQueryableGameLibraries();
        Task<Library?> GetLibraryByIdAsync(Guid id);
        Task<Library?> GetLibraryByUserAsync(string userId);
        Task AddLibraryAsync(Library Library);
        void Delete(Library library);
        void Update(Library library);
        IQueryable<Game> GetGamesInLibraryAsync();
        Task AddGameInLibraryAsync(GameLibrary game);
        void DeleteGameInLibrary(GameLibrary game);
        void HideGameInLibrary(GameLibrary game);
        void UpdateGameLibrary(GameLibrary gameLibrary);
        Task<GameLibrary?> GetGameLibraryByIdAsync(Guid libraryId,Guid gameId);
        Task SaveChangesAsync();
    }
}
