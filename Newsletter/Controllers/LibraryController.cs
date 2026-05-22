using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newsletter.Data;
using Newsletter.DTOs.Libraries;
using Newsletter.Models;
using Newsletter.Services;
using System.Security.Claims;

namespace Newsletter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LibraryController : Controller
    {
        private readonly ILibraryService _libraryService;
        public LibraryController(ILibraryService libraryService)
        {
            _libraryService = libraryService;
        }

        [Authorize(Roles ="Admin, Moderador")]
        [HttpGet("GET/Libraries")]
        public async Task<IActionResult> GetLibraries()          
        {
            var libraries = await _libraryService.GetLibrariesAsync();
            return Ok(libraries);
        }
        [HttpGet("GET/Library")]
        public async Task<IActionResult> GetLibrary([FromQuery] string? userId)
        {
            var loginUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(loginUserId == null)
            {
                return Unauthorized("Usuario no autenticado");
            }
            string targetUserId = string.IsNullOrEmpty(userId) ? loginUserId : userId;
            if(targetUserId != loginUserId)
            {
                bool isAdmin = User.IsInRole("Admin");
                bool isModerator = User.IsInRole("Moderador");
                if ( !isAdmin || !isModerator)
                {
                    return Forbid("No tienes permisos para ver la biblioteca de este usuario");
                }
            }
            var library = await _libraryService.GetLibraryAsync(targetUserId);
            if (library == null)
            {
                return NotFound("No se encontró ima biblioteca para este usuario");
            }
            return Ok(library);
        }

        [Authorize]
        [HttpGet("GET/Library/Games")]
        public async Task<IActionResult> GetGamesInLibrary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("Usuario no autenticado");
            }
            var result = await _libraryService.GetMyGamesAsync(userId);
            if(!result.success)
            {
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.Games);
        }

        [Authorize]
        [HttpPost("Create/Library")]
        public async Task<IActionResult> CreateLibrary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("Usuario no autenticado");
            }
            var result = await _libraryService.CreateLibraryAsync(userId);
            if (!result.success)
            {
                return BadRequest(result.ErrorMessage);
            }
            return Created("", new
                {
                mensaje = "Biblioteca creada exitosamente",
                libraryId = result.LibraryId
            });

        }

        [Authorize]
        [HttpPost("AddGame/Library")]
        public async Task<IActionResult> AddGameToLibrary(AddGameLibraryDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("Usuario no autenticado");
            }
            var result = await _libraryService.AddGameInLibraryAsync(dto, userId);
            if (!result.success)
            {
                return BadRequest(result.ErrorMessage);
            }
            return Created("", new
            {
                mensaje = "Juego agregado a la biblioteca exitosamente",
                gameId = result.gameId
            });
        }
       /* [Authorize]
        [HttpPut("RemoveGame/Library")]
        public async Task<IActionResult> RemoveGameFromLibrary(AddGameLibraryDto dto, bool state)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("Usuario no autenticado");
            }
            bool isAdmin = User.IsInRole("Admin");
            bool isModerator = User.IsInRole("Moderador");
            var library = await _context.Libraries.FirstOrDefaultAsync(l => l.UserId == userId);
            if (library == null || library.UserId != userId || !isAdmin || !isModerator)
            {
                return Forbid("No tienes permiso para modificar esta biblioteca");
            }
            var gameLibrary = await _context.GameLibraries.FirstOrDefaultAsync(gl => gl.GameId == dto.GameId && gl.LibraryId == dto.LibraryId);
            if (gameLibrary == null)
            {

                return BadRequest("El juego no está en tu biblioteca");
            }
            gameLibrary.State = state;
            await _context.SaveChangesAsync();
            return Ok("Juego Eliminado de tu biblioteca");
        }

        private bool LibraryExists(string userId)
        {
            return _context.Libraries.Any(l => l.UserId == userId);
        }*/

    }
}

