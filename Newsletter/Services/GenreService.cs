using Microsoft.EntityFrameworkCore;
using Newsletter.DTOs.Genres;
using Newsletter.Models;
using Newsletter.Repositories;

namespace Newsletter.Services
{
    public class GenreService : IGenreService
    {
        private readonly IGenreRepository _repository;
        public GenreService(IGenreRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<GenreDto>> GetGenresAsync(int? id, string? name, bool? state)
        {
            var query = _repository.GetQueryable();
            if (id > 0)
            {
                query = query.Where(g => g.Id == id);
            }
            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(g => g.Description.ToLower().Contains(name.ToLower()));
            }
            query = query.Where(g => g.State == state);
            return await query.Select(g => new GenreDto
            {
                Id = g.Id,
                Description = g.Description,
                State = g.State
            }).ToListAsync();
        }

        public async Task<(bool Success, string ErrorMessage, Genre? Genre)> CreateGenreAsync(GenreCreateDto dto)
        {
            if (await _repository.GenreExists(dto.Description))
            {
                return (false, "Ya existe un género con ese nombre", null);
            }
            var genre = new Genre
            {
                Description = dto.Description,
                State = true
            };
            await _repository.AddAsync(genre);
            await _repository.SaveChangesAsync();
            return (true, string.Empty, genre);
        }

        public async Task<bool> DeleteGenreAsync(int id)
        {
            var genre = await _repository.GetByIdAsync(id);
            if (genre == null)
            {
                throw new KeyNotFoundException("Género no encontrado");
            }
            _repository.Delete(genre);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<Genre> UpdateGenreAsync(int id, GenreDto dto)
        {
            var genre = await _repository.GetByIdAsync(id);
            if (genre == null)
            {
                throw new KeyNotFoundException("Género no encontrado");
            }
            genre.Description = dto.Description;
            genre.State = dto.State;
            _repository.Update(genre);
            await _repository.SaveChangesAsync();
            return genre;
        }

        public async Task<Genre> HideGenre(int id, bool state)
        {
            var genre = await _repository.GetByIdAsync(id);
            if(genre == null)
            {
                throw new KeyNotFoundException("Género no encontrado");
            }
            genre.State = state;
            await _repository.SaveChangesAsync();
            return genre;               
        }
    }
}
