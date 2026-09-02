using Humanizer;
using Microsoft.EntityFrameworkCore;
using Newsletter.DTOs.Reviews;
using Newsletter.Models;
using Newsletter.Repositories;

namespace Newsletter.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _repository;
        public ReviewService(IReviewRepository repository)
        {
            _repository = repository;
        }
        public async Task<List<ReviewResponseDto>> GetReviewsAsync(Guid? id, Guid? gameId, string? userId)
        {
            var query = _repository.GetQueryable();
            if (id.HasValue)
            {
                query = query.Where(r => r.Id == id.Value);
            }
            if (gameId.HasValue)
            {
                query = query.Where(r => r.GameId == gameId.Value);
            }
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(r => r.UserId == userId);
            }
            return await query
                .Select(r => new ReviewResponseDto
                {
                    Id = r.Id,
                    UserName = r.User.UserName,
                    GameName = r.Game.Name,
                    Rating = r.Rating,
                    State = r.State,
                    Comment = r.Comment,
                    ReviewDate = r.ReviewDate
                })
                .ToListAsync();
        }
        public async Task<(bool Success, string ErrorMessage, Review? Review)> CreateReviewAsync(ReviewCreateDto dto, string userId)
        {
            if (await _repository.ReviewExists(userId, dto.GameId))
            {
                return (false, "Ya has dejado una reseña para este juego", null);
            }
            var review = new Review
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                GameId = dto.GameId,
                Rating = dto.Rating,
                State = true,
                Comment = dto.Comment,
                ReviewDate = DateTime.UtcNow
            };
            await _repository.AddAsync(review);
            await _repository.SaveChangesAsync();
            return (true, string.Empty, review);
        }
        public async Task<bool> DeleteReviewAsync(Guid id, string userId, bool isAdmin, bool isModerator)
        {
            var review = await _repository.GetByIdAsync(id);
            if (review == null)
            {
                throw new KeyNotFoundException("Reseña no encontrada");
            }
            if (review.UserId != userId && !isAdmin && !isModerator)
            {
                throw new UnauthorizedAccessException("No tienes permiso para eliminar esta reseña");
            }
            _repository.Delete(review);
            await _repository.SaveChangesAsync();
            return true;
        }
        public async Task<Review> UpdateReviewAsync( Guid id, ReviewUpdateDto dto, string userId)
        {
            var review = await _repository.GetByIdAsync(id);
            if (review == null)
            {
                throw new KeyNotFoundException("Reseña no encontrada");
            }
            if (review.UserId != userId)
            {
                throw new UnauthorizedAccessException("No tienes permiso para editar esta reseña");
            }
            review.Comment = dto.Comment;
            review.Rating = dto.Rating;
            review.ReviewDate = DateTime.UtcNow;
            _repository.Update(review);
            await _repository.SaveChangesAsync();
            return review;
        }
        public async Task<Review> HideReviewAsync(Guid id, bool state, bool isAdmin, bool isModerator)
        {
            var review = await _repository.GetByIdAsync(id);
            if (review == null)
            {
                throw new KeyNotFoundException("Reseña no encontrada");
            }
            if (!isAdmin && !isModerator)
            {
                throw new UnauthorizedAccessException("No tienes permiso para editar esta reseña");
            }
            review.State = state;
            _repository.Update(review);
            await _repository.SaveChangesAsync();
            return review;
        }
    }
}
