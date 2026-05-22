using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newsletter.Data;
using Newsletter.DTOs.Platforms;
using Newsletter.Models;
using Newsletter.Services;

namespace Newsletter.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PlatformController : Controller
    {
        private readonly IPlatformService _platformService;
        public PlatformController(IPlatformService platformService)
        {
            _platformService = platformService;
        }
        /// Gets
        [HttpGet("GET/platforms")]

        public async Task<IActionResult> Platforms(
            [FromQuery] int? id,
            [FromQuery] string? name,
            [FromQuery] bool? state)
        {
            bool isAdmin = User.IsInRole("Admin");
            bool isModerator = User.IsInRole("Moderador");
            bool? stateFinal;

            if (isAdmin || isModerator)
            {
                stateFinal = state;
            }
            else
            {
                stateFinal = true;
            }
            var platform = await _platformService.GetPlatformsAsync(id, name, stateFinal);
            if (platform.Count == 0)
            {
                return NotFound("Plataforma no encontrada");
            }
            return Ok(platform);

        }
        /// posts
        [Authorize(Roles = "Admin, Moderador")]
        [HttpPost("Create/platform")]
        public async Task<IActionResult> Create(PlatformCreateDto dto)
        {
            try
            {
                var platform = await _platformService.CreatePlatformAsync(dto);
                return Ok(platform);
            }
            catch (ArgumentException ex)
            {

                return BadRequest(ex.Message);
            }
        }

        /// Puts
        [Authorize(Roles = "Admin, Moderador")]
        [HttpPut("Edit/platform/{id}")]
        public async Task<IActionResult> Update(int id, PlatformDto dto)
        {
            try
            {
                var platform = await _platformService.UpdatePlatformAsync(id, dto);
                return Ok(platform);

            }
            catch (KeyNotFoundException ex)
            {

                return NotFound(ex.Message);
            }
        }
        [Authorize(Roles = "Admin, Moderador")]
        [HttpPut("Edit/state/platform/{id}")]
        public async Task<IActionResult> UpdateState(int id, bool state)
        {
            try
            {
                var platform = await _platformService.HidePlatform(id, state);
                return Ok(platform);
            }
            catch (KeyNotFoundException ex)
            {

                return NotFound(ex.Message);
            }
        }
        [Authorize(Roles = "Admin, Moderador")]
        [HttpDelete("Delete/platform/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var delete = await _platformService.DeletePlatformAsync(id);
                if (delete)
                {
                    return Ok("Plataforma borrada exitosamente");
                }
                return BadRequest("Hubo un error al tratar de eliminar la plataforma");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
