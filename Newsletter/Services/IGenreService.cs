using Newsletter.DTOs.Genres;
using Newsletter.Models;

namespace Newsletter.Services
{
    public interface IGenreService
    {
        Task<List<GenreDto>> GetGenresAsync(int? id, string? name, bool? state);
        Task<(bool Success, string ErrorMessage, Genre? Genre)> CreateGenreAsync(GenreCreateDto dto);
        Task<bool> DeleteGenreAsync(int id);
        Task<Genre> UpdateGenreAsync(int id, GenreDto dto);
        Task<Genre> HideGenre(int id, bool state);
    }
}
