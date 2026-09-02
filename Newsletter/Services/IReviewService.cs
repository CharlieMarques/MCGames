using Newsletter.DTOs.Reviews;
using Newsletter.Models;


namespace Newsletter.Services
{
    public interface IReviewService
    {
        Task<List<ReviewResponseDto>> GetReviewsAsync(Guid? id, Guid? gameId, string? userId);
        Task<(bool Success, string ErrorMessage, Review? Review)> CreateReviewAsync(ReviewCreateDto dto, string userId);
        Task<bool> DeleteReviewAsync(Guid id, string userId, bool isAdmin, bool isModerator);
        Task<Review> UpdateReviewAsync(Guid id, ReviewUpdateDto dto, string userId);
        Task<Review> HideReviewAsync(Guid id, bool state, bool isAdmin, bool isModerator);
    }
}
