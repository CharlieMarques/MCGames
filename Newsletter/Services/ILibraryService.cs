
using Newsletter.DTOs.Libraries;
using Newsletter.Models;

namespace Newsletter.Services
{
    public interface ILibraryService
    {
        Task<List<LibraryReadDto>> GetLibrariesAsync();
        Task<LibraryReadDto?> GetLibraryAsync(string userId);
        Task<(bool success, string ErrorMessage, Guid? LibraryId)> CreateLibraryAsync(string userId);
        Task<bool> DeleteLibraryAsync(Guid id);
        Task<(bool success, string ErrorMessage,Guid? gameId)> AddGameInLibraryAsync(AddGameLibraryDto dto, string userId);
        Task<bool> DeleteGameInLibraryAsync(Guid libraryId, Guid gameId);
        Task<bool> HideGameInLibrary(Guid libraryId, Guid gameId, bool state);
        Task<(bool success, string ErrorMessage,List<ReadGamesLibraryDto> Games)>GetMyGamesAsync(string userId);
    }
}
