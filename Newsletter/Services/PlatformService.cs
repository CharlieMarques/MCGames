using Microsoft.EntityFrameworkCore;
using Newsletter.DTOs.Platforms;
using Newsletter.Models;
using Newsletter.Repositories;

namespace Newsletter.Services
{
    public class PlatformService : IPlatformService

    {
        private readonly IPlatformRepository _repository;

        public PlatformService(IPlatformRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<PlatformDto>> GetPlatformsAsync(int? id, string? name, bool? state)
        {
            var query = _repository.GetQueryable();
            if (id > 0)
            {
                query = query.Where(p => p.Id == id);
            }
            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(p => p.Description.ToLower().Contains(name.ToLower()));
            }
            query = query.Where(p => p.State == state);
            return await query.Select(p => new PlatformDto
            {
                Id = p.Id,
                Description = p.Description,
                UrlLogo = p.UrlLogo,
                State = p.State
            }).ToListAsync();
        }
        public async Task<(bool Success, string ErrorMessage, Platform? Platform)> CreatePlatformAsync(PlatformCreateDto dto)
        {
            if (await _repository.PlatformExists(dto.Description))
            {
                return (false, "Ya existe una plataforma con esa descripción", null);
            }
            var platform = new Platform
            {
                Description = dto.Description,
                UrlLogo = dto.UrlLogo,
                State = true
            };
            await _repository.AddAsync(platform);
            await _repository.SaveChangesAsync();
            return (true, string.Empty, platform);
        }
        public async Task<bool> DeletePlatformAsync(int id)
        {
            var platform = await _repository.GetByIdAsync(id);
            if (platform == null)
            {
                throw new KeyNotFoundException("Plataforma no encontrada");
            }
            _repository.Delete(platform);
            await _repository.SaveChangesAsync();
            return true;
        }
        public async Task<Platform> UpdatePlatformAsync(int id, PlatformDto dto)
        {
            var platform = await _repository.GetByIdAsync(id);
            if (platform == null)
            {
                throw new KeyNotFoundException("Plataforma no encontrada");
            }
            platform.Description = dto.Description;
            platform.UrlLogo = dto.UrlLogo;
            platform.State = dto.State;
            _repository.Update(platform);
            await _repository.SaveChangesAsync();
            return platform;
        }
        public async Task<Platform> HidePlatform(int id, bool state)
        {
            var platform = await _repository.GetByIdAsync(id);
            if (platform == null)
            {
                throw new KeyNotFoundException("Plataforma no encontrada");
            }
            platform.State = state;
            _repository.Update(platform);
            await _repository.SaveChangesAsync();
            return platform;
        }
    }
}
