using Newsletter.DTOs.Platforms;
using Newsletter.Models;

namespace Newsletter.Services
{
    public interface IPlatformService
    {
        Task<List<PlatformDto>> GetPlatformsAsync(int? id, string? name,bool? state);
        Task<(bool Success, string ErrorMessage, Platform? Platform)> CreatePlatformAsync(PlatformCreateDto dto);
        Task<bool>DeletePlatformAsync(int id);
        Task<Platform> UpdatePlatformAsync(int id, PlatformDto dto);
        Task<Platform> HidePlatform(int id, bool state);
    }
}
