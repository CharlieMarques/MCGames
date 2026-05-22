using Microsoft.EntityFrameworkCore;
using Newsletter.DTOs.Games;
using Newsletter.DTOs.Genres;
using Newsletter.DTOs.Platforms;
using Newsletter.Models;
using Newsletter.Repositories;

namespace Newsletter.Services
{
    public class GameService : IGameService
    {
        private readonly IGameRepository _repository;
        private readonly IPlatformRepository _platformRepository;
        private readonly IGenreRepository _genreRepository;
        public GameService(IGameRepository repository, IPlatformRepository platformRepository, IGenreRepository genreRepository)
        {
            _repository = repository;
            _platformRepository = platformRepository;
            _genreRepository = genreRepository;
        }
        public async Task<(bool Success, string ErrorMessage, Game? game)> CreateGameAsync(GameCreateDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException("Error no esperado");
            }
            bool genreValid = await _genreRepository.AllExistAsync(dto.GenreIds);
            if (!genreValid)
            {
                throw new ArgumentException("Uno o más géneros proporcionados no existen");
            }
            bool platformValid = await _platformRepository.AllExistAsync(dto.PlatformsIds);
            if(!platformValid)
            {
                throw new ArgumentException("Una o más plataformas proporcionadas no existen");
            }

            var game = new Game
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                ShortDescription = dto.ShortDescription,
                ReleaseDate = dto.ReleaseDate,
                GameCoverUrl = dto.GameCoverUrl,
                Price = dto.Price,
                OnOffer = dto.OnOffer,
                State = true,

                GameGenre = new List<GameGenre>(),
                GamePlatforms = new List<GamePlatform>()
            };

            if (dto.GenreIds != null && dto.GenreIds.Any())
            {
                foreach (var genreId in dto.GenreIds)
                {
                    game.GameGenre.Add(new GameGenre
                    {
                        GameId = game.Id,
                        GenreId = genreId
                    });
                }
            }
            if (dto.PlatformsIds != null && dto.PlatformsIds.Any())

            {
                foreach (var platformId in dto.PlatformsIds)
                {
                    game.GamePlatforms.Add(new GamePlatform
                    {
                        GameId = game.Id,
                        PlatformId = platformId
                    });
                }
            }

            await _repository.AddAsync(game);
            await _repository.SaveChangesAsync();
            return (true, string.Empty, game);
        }

        public async Task<bool> DeleteGameAsync(Guid id)
        {
            var game = await _repository.GetByIdAsync(id);
            if (game == null)
            {
                throw new KeyNotFoundException("Juego no encontrado");
            }
            _repository.Delete(game);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<GameReadDto>> GetGamesAsync(Guid? id, string? name, bool? state, List<int>? genreIds,string sortBy,int page, int pageSize)
        {
            var query = _repository.GetQueryable();
            if (id.HasValue)
            {
                query = query.Where(g => g.Id == id);
                page = 1;
                pageSize = 1;
            }
            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(g => g.Name.Contains(name));
            }
            if (state.HasValue)
            {
                query = query.Where(g => g.State == state.Value);
            }
            if(genreIds != null && genreIds.Any())
            {
                query = query.Where(g => g.GameGenre.Any(gg => genreIds.Contains(gg.Genre.Id)));
            }
            if(sortBy == "releasedate_desc")
            {
                var hoy = DateTime.UtcNow;
                query = query.Where(g => g.ReleaseDate <= hoy);
            }
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(g => g.Price),
                "price_desc" => query.OrderByDescending(g => g.Price),
                "name_desc" => query.OrderByDescending(g => g.Name),
                "date_desc" => query.OrderByDescending(g => g.ReleaseDate),
                "releasedate_desc" => query.OrderByDescending(g => g.ReleaseDate),
                _ => query.OrderBy(g => g.Name)
               // _ => query.OrderBy(g => g.Id)
            };
            int totalRecords = await query.CountAsync();

            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            var items =  await query
                
                    .Skip((page -1)* pageSize)
                    .Take(pageSize)
              .Select(g => new GameReadDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    State = g.State,
                    ShortDescription = g.ShortDescription,
                    ReleaseDate = g.ReleaseDate,
                    GameCoverUrl = g.GameCoverUrl,
                    Price = g.Price,
                    OnOffer = g.OnOffer,

                  Genres = g.GameGenre.Select(gg => new GenreDto
                  {
                      Id = gg.Genre.Id,
                      Description = gg.Genre.Description
                  }).ToList(),

                  Platforms = g.GamePlatforms.Select(gp => new PlatformDto
                  {
                      Id = gp.Platform.Id,
                      Description = gp.Platform.Description
                  }).ToList(),
                    TotalReviews =g.Reviews.Count() ,
                    AverageRating = g.Reviews.Any() ? g.Reviews.Average(r => r.Rating) :0
                })
              .AsSplitQuery()
              .ToListAsync();
            return new PagedResult<GameReadDto>
            {
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                CurrentPage = page,
                Items = items,
            };
        }

        public async Task<Game> HideGame(Guid id, bool state)
        {
            var game = await _repository.GetByIdAsync(id);

            if (game == null)
            {
                throw new KeyNotFoundException("Juego no encontrado");
            }
            game.State = state;
            _repository.Update(game);
            await _repository.SaveChangesAsync();
            return game;

        }

        public async Task<Game> UpdateGameAsync(Guid id, GameUpdateDto dto)
        {
            var game = await _repository.GetByIdAsync(id);

            if (game == null)
            {
                throw new KeyNotFoundException("Juego no encontrado");
            }
            game.Name = dto.Name;
            game.ShortDescription = dto.ShortDescription;
            game.ReleaseDate = dto.ReleaseDate;
            game.GameCoverUrl = dto.GameCoverUrl;
            game.Price = dto.Price;
            game.State = dto.State;
            game.OnOffer = dto.OnOffer;
            _repository.Update(game);
            await _repository.SaveChangesAsync();
            return game;
        }
    }
}
