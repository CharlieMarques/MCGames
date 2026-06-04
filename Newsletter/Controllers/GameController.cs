using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newsletter.Data;
using Newsletter.DTOs.Games;
using Newsletter.Models;
using Newsletter.Services;

namespace Newsletter.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class GameController : Controller
    {
        private readonly IGameService _gameService;
        public GameController(IGameService gameService)
        {
            _gameService = gameService;
        }

        /// Gets

        [HttpGet("GET/games")]

        public async Task<IActionResult> Games(
            [FromQuery] Guid? id,
            [FromQuery] string? name,
            [FromQuery] bool? state,
            [FromQuery] bool? onOffer,
            [FromQuery] List<int>? genreIds,
            [FromQuery] List<int>? categoryIds,
            [FromQuery] string? sortBy,
            [FromQuery] int page =1,
            [FromQuery] int pageSize =10)
        {
            bool isAdmin = User.IsInRole("Admin");
            bool isModerator = User.IsInRole("Moderador");
            bool? stateFinal;
            if(isAdmin || isModerator)
            {
                stateFinal = state;
            }
            else
            {
                stateFinal = true;
            }
            if (string.IsNullOrEmpty(sortBy))
            {
                sortBy = "name_asc";
            }
            var games = await _gameService.GetGamesAsync(id, name, stateFinal,onOffer,genreIds,categoryIds,sortBy,page,pageSize);
            return Ok(games);
        }

        [HttpGet("GET/MultiplatformGames")]
        public async Task<IActionResult> GetJuegosMultiplataforma()
        {
            try
            {
                // 💡 Buscamos los juegos que tienen cargados datos de Steam Y de Epic al mismo tiempo
                var juegosCruzados = await _gameService.GetMultiPlatformGamesAsync();

                return Ok(juegosCruzados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener los juegos unificados: {ex.Message}");
            }
        }

        /// Posts
        [Authorize(Roles = "Admin, Moderador")]
        [HttpPost("Create/game")]
        public async Task<IActionResult> Create(GameCreateDto dto)
        {
            try
            {
                var game = await _gameService.CreateGameAsync(dto);
                return Ok(game);
            }
            catch (ArgumentException ex)
            {

                return BadRequest(ex.Message);
            }
        }

        /// Puts
        [Authorize(Roles = "Admin, Moderador")]
        [HttpPut("Edit/game/{id}")]
        public async Task<IActionResult> Edit(Guid id, GameUpdateDto dto)
        {
            try
            {
                var updatedGame = await _gameService.UpdateGameAsync(id, dto);
                return Ok(updatedGame);
            }
            catch (KeyNotFoundException ex)
            {

                return NotFound(ex.Message);
            }
        }
        [Authorize(Roles = "Admin, Moderador")]
        [HttpPut("Edit/state/game/{id}")]
        public async Task<IActionResult> ChangeStatus(Guid id, bool state)
        {
            try
            {
                var HideGame = await _gameService.HideGame(id, state);
                return Ok(HideGame);
            }
            catch (KeyNotFoundException ex)
            {

                return NotFound(ex.Message);
            }
        }

        /// Deletes
        [Authorize(Roles = "Admin, Moderador")]
        [HttpDelete("Delete/game/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var delete = await _gameService.DeleteGameAsync(id);
                if (delete)
                {
                    return Ok("Juego borrado exitosamente");
                }
                return BadRequest("Hubo un error al tratar de eliminar el juego");
            }
            catch (KeyNotFoundException ex)
            {

                return NotFound(ex.Message);
            }
        }
    }
}
