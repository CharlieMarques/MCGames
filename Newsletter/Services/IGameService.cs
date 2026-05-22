using Newsletter.DTOs.Games;
using Newsletter.DTOs.Reviews;
using Newsletter.Models;

namespace Newsletter.Services
{
    public interface IGameService
    {
        Task<PagedResult<GameReadDto>> GetGamesAsync(Guid? id, string? name, bool? state, List<int>? genreIds, string? sortBy, int page, int pageSize);
        Task<(bool Success, string ErrorMessage, Game? game)> CreateGameAsync(GameCreateDto dto);
        Task<bool> DeleteGameAsync(Guid id);
        Task<Game> UpdateGameAsync(Guid id, GameUpdateDto dto);
        Task<Game> HideGame(Guid id, bool state);
    }

}
