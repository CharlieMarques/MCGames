using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newsletter.Data;
using Newsletter.Models;
using System.Security.Claims;
using Newsletter.DTOs.Reviews;
using Newsletter.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Newsletter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReviewsController : Controller
    {
        private readonly IReviewService _service;
        public ReviewsController( IReviewService service)
        {
            _service = service;
        }

        [HttpGet("GET/reviews")]
        public async Task<IActionResult> GetReviews(
            [FromQuery] Guid? id,
            [FromQuery] Guid? gameId,
            [FromQuery] string? userId)
        {
            var reviews = await _service.GetReviewsAsync(id, gameId, userId);
            {
                if (reviews.Count == 0)
                {
                    return Ok(new List<ReviewResponseDto>());
                }
                return Ok(reviews);
            }
        }

        [Authorize]
        [HttpPost("Create/review")]
        public async Task<IActionResult> CreateReview(ReviewCreateDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Datos inválidos");
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("Usuario no autenticado");
            }
            var result = await _service.CreateReviewAsync(dto, userId);
            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.Review);
        }

        [Authorize]
        [HttpPut("Edit/review/{id}")]
        public async Task<IActionResult> UpdateReview(Guid id, ReviewUpdateDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Datos inválidos");
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("Usuario no autenticado");
            }
            try
            {
                var updatedReview = await _service.UpdateReviewAsync(id, dto, userId);
                return Ok(updatedReview);
            }
            catch (KeyNotFoundException ex)
            {

                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [Authorize(Roles = "Admin, Moderador")]
        [HttpPut("Hide/review/{id}")]
        public async Task<IActionResult> HideReview(Guid id, bool state)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("Usuario no autenticado");
            }
            bool isAdmin = User.IsInRole("Admin");
            bool isModerator = User.IsInRole("Moderator");
            try
            {
                var review = await _service.HideReviewAsync(id, state, isAdmin, isModerator);
                return Ok(review);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [Authorize]
        [HttpDelete("Delete/review/{id}")]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");
            bool isModerator = User.IsInRole("Moderator");
            try
            {
                await _service.DeleteReviewAsync(id, userId, isAdmin, isModerator);
                return Ok("Reseña eliminada exitosamente");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

    }

}
