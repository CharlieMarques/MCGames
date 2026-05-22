using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newsletter.Data;
using Newsletter.DTOs.Genres;
using Newsletter.Models;
using Newsletter.Services;

namespace Newsletter.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class GenreController : ControllerBase
    {
        private readonly IGenreService _genreService;
        public GenreController(IGenreService genreService)
        {
            _genreService = genreService;
        }
        [HttpGet("GET/genres")]
        public async Task<IActionResult> Genres(
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
            var genre = await _genreService.GetGenresAsync(id, name, stateFinal);
            if (genre.Count == 0)
            {
                return NotFound("Género inexistente");
            }
            return Ok(genre);
        }

        [Authorize(Roles = "Admin, Moderador")]
        [HttpPost("Create/genre")]
        public async Task<IActionResult> Create(GenreCreateDto dto)
        {
            try
            {
                var genre = await _genreService.CreateGenreAsync(dto);
                return Ok(genre);
            }
            catch (ArgumentException ex)
            {

                return BadRequest(ex.Message);
            }
        }
        [Authorize(Roles = "Admin, Moderador")]
        [HttpPut("Edit/genre/{id}")]
        public async Task<IActionResult> Edit(int id, GenreDto dto)
        {
            try
            {
                var updateGenre = await _genreService.UpdateGenreAsync(id, dto);
                return Ok(updateGenre);
            }
            catch (KeyNotFoundException ex)
            {

                return NotFound(ex.Message);
            }
        }
        [Authorize(Roles = "Admin, Moderador")]
        [HttpPut("Edit/state/genre/{id}")]
        public async Task<IActionResult> EditState(int id, bool state)
        {
            try
            {
                var genre = await _genreService.HideGenre(id, state);
                return Ok(genre);
            }
            catch (KeyNotFoundException ex)
            {

                return NotFound(ex.Message);
            }
        }
        [Authorize(Roles = "Admin, Moderador")]
        [HttpDelete("Delete/genre/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var delete = await _genreService.DeleteGenreAsync(id);
                if (delete)
                {
                    return Ok("Género borrado exitosamente");
                }
                return BadRequest("Hubo un error al tratar de eliminar el género");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

        }
    }

}
