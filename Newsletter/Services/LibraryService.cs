using Microsoft.EntityFrameworkCore;
using Newsletter.DTOs.Libraries;
using Newsletter.Models;
using Newsletter.Repositories;

namespace Newsletter.Services
{
    public class LibraryService : ILibraryService
    {
        private readonly ILibraryRepository _libraryRepository;
        public LibraryService(ILibraryRepository libraryRepository)
        {
            _libraryRepository = libraryRepository;
        }
        public async Task<(bool success, string ErrorMessage, Guid? gameId)> AddGameInLibraryAsync(AddGameLibraryDto dto, string userId)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }
            var library = await _libraryRepository.GetLibraryByUserAsync(userId);
            if (library == null)
            {
                return (false, "No se encontró una biblioteca asociada a tu cuenta.", null);
            }
            var gameLibrary = await _libraryRepository.GetGameLibraryByIdAsync(library.Id, dto.GameId);

            if (gameLibrary != null)
                if (gameLibrary.State == true)
                {
                    return (false, "El juego ya está en tu biblioteca.", null);
                }
                else
                {
                    gameLibrary.State = true;
                    gameLibrary.AddedDate = DateTime.UtcNow;
                    await _libraryRepository.SaveChangesAsync();
                    return (true, string.Empty, dto.GameId);
                }
            var newGameLibrary = new GameLibrary
            {
                GameId = dto.GameId,
                LibraryId = library.Id,
                State = true,
                AddedDate = DateTime.Now
            };
            await _libraryRepository.AddGameInLibraryAsync(newGameLibrary);
            await _libraryRepository.SaveChangesAsync();
            return (true, string.Empty, dto.GameId);
        }

        public async Task<(bool success, string ErrorMessage, Guid? LibraryId)> CreateLibraryAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return (false, "Id de usuario inválido",null);
            }
            var existingLibrary = await _libraryRepository.GetLibraryByUserAsync(userId);
            if (existingLibrary != null)
            {
                return (false, "Ya tienes una biblioteca creada", null);
            }
            var newLibrary = new Library
            {
                Id = Guid.NewGuid(),
                UserId = userId
            };
            await _libraryRepository.AddLibraryAsync(newLibrary);
            await _libraryRepository.SaveChangesAsync();
            return (true, string.Empty,newLibrary.Id);
        }
        public async Task<bool> DeleteGameInLibraryAsync(Guid libraryId, Guid gameId)
        {
            var gameLibrary = await _libraryRepository.GetGameLibraryByIdAsync(libraryId,gameId);
            if (gameLibrary == null)
            {
                throw new KeyNotFoundException("Juego no encontrado en la biblioteca");
            }
            _libraryRepository.DeleteGameInLibrary(gameLibrary);
            await _libraryRepository.SaveChangesAsync();
            return true;

        }

        public async Task<bool> DeleteLibraryAsync(Guid id)
        {
            var library = await _libraryRepository.GetLibraryByIdAsync(id);
            if (library == null)
            {
                throw new KeyNotFoundException("Biblioteca no encontrada");
            }
            _libraryRepository.Delete(library);
            await _libraryRepository.SaveChangesAsync();
            return true;
        }

        public async Task<(bool success, string ErrorMessage, List<ReadGamesLibraryDto> Games)> GetMyGamesAsync(string userId)
        {
            var library = await _libraryRepository.GetLibraryByUserAsync(userId);
            if(library == null)
            {
                return (true, string.Empty, new List<ReadGamesLibraryDto>());
            }
            var games = await _libraryRepository.GetQueryableGameLibraries()
                .Include(gl => gl.Game)
                .Where(gl => gl.LibraryId == library.Id && gl.State == true)
                .Select(gl => new ReadGamesLibraryDto
                {
                    GameId = gl.GameId,
                    Name = gl.Game != null ? gl.Game.Name : "Juego Desconocido",
                    ReleaseDate = gl.AddedDate,
                    GameCoverUrl = gl.Game != null ? gl.Game.GameCoverUrl : string.Empty,
                    SteamAppId = gl.Game != null ? gl.Game.SteamAppId : 0
                })
                .ToListAsync();
            return (true, string.Empty, games);
        }

        public async Task<List<LibraryReadDto>> GetLibrariesAsync()
        {
            return await _libraryRepository.GetQueryableLibraries()
                .Select(l => new LibraryReadDto
                {
                    Id = l.Id,
                    UserId = l.UserId,
                    UserName = l.User != null ? (l.User.UserName ?? "Usuario Desconocido") : "Usuario Desconocido",
                    GameNames = l.GameLibraries != null
                    ? l.GameLibraries.Where(gl => gl.State == true)
                    .Select(gl => gl.Game != null ? gl.Game.Name : "Juego Desconocido").ToList()
                    : new List<string>()
                }).ToListAsync();
        }

        public async Task<LibraryReadDto?> GetLibraryAsync(string userId)
        {
            var query = _libraryRepository.GetQueryableLibraries();
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(l => l.UserId == userId);
            }
            return await query.Select(l => new LibraryReadDto
            {
                Id = l.Id,
                UserId = l.UserId,
                UserName = l.User != null ? (l.User.UserName ?? "Usuario Desconocido") : "Usuario Desconocido ",
                GameNames = l.GameLibraries != null ?
                l.GameLibraries.Where(gl => gl.State == true)
                .Select(gl => gl.Game != null ? gl.Game.Name : "Juego Desconocido")
                .ToList()
                : new List<string>()
            }).FirstOrDefaultAsync();
        }

        public async Task<bool> HideGameInLibrary(Guid libraryId, Guid gameId, bool state)
        {
            var gameLibrary = await _libraryRepository.GetGameLibraryByIdAsync(libraryId,gameId);
            if (gameLibrary == null)
            {
                throw new KeyNotFoundException("Juego no encontrado");
            }
            gameLibrary.State = state;
            _libraryRepository.UpdateGameLibrary(gameLibrary);
            await _libraryRepository.SaveChangesAsync();
            return true;
        }
    }
}
