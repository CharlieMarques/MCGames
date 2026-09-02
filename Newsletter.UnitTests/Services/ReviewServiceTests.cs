using Moq;
using Xunit;
using Newsletter.Services;
using Newsletter.Repositories;
using Newsletter.Models;
using Newsletter.DTOs.Reviews;

namespace Newsletter.UnitTests.Services
{
    public class ReviewServiceTests
    {
        private readonly Mock<IReviewRepository> _mockRepo;
        private readonly ReviewService _service;

        public ReviewServiceTests()
        {
            _mockRepo = new Mock<IReviewRepository>();
            _service = new ReviewService(_mockRepo.Object);
        }

        // ---------- DeleteReviewAsync ----------

        [Fact]
        public async Task DeleteReviewAsync_CuandoNoExiste_LanzaKeyNotFoundException()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                     .ReturnsAsync((Review)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.DeleteReviewAsync(Guid.NewGuid(), "user1", isAdmin: false, isModerator: false));
        }

        [Fact]
        public async Task DeleteReviewAsync_CuandoNoEsElDuenioNiTienePermiso_LanzaUnauthorizedAccessException()
        {
            var review = new Review { Id = Guid.NewGuid(), UserId = "otroUsuario" };
            _mockRepo.Setup(r => r.GetByIdAsync(review.Id)).ReturnsAsync(review);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.DeleteReviewAsync(review.Id, "user1", isAdmin: false, isModerator: false));
        }

        [Fact]
        public async Task DeleteReviewAsync_CuandoEsElDuenio_BorraYDevuelveTrue()
        {
            var review = new Review { Id = Guid.NewGuid(), UserId = "user1" };
            _mockRepo.Setup(r => r.GetByIdAsync(review.Id)).ReturnsAsync(review);

            var result = await _service.DeleteReviewAsync(review.Id, "user1", isAdmin: false, isModerator: false);

            Assert.True(result);
            _mockRepo.Verify(r => r.Delete(review), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteReviewAsync_CuandoEsAdminYNoEsElDuenio_BorraIgual()
        {
            var review = new Review { Id = Guid.NewGuid(), UserId = "otroUsuario" };
            _mockRepo.Setup(r => r.GetByIdAsync(review.Id)).ReturnsAsync(review);

            var result = await _service.DeleteReviewAsync(review.Id, "user1", isAdmin: true, isModerator: false);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteReviewAsync_CuandoEsModeradorYNoEsElDuenio_BorraIgual()
        {
            var review = new Review { Id = Guid.NewGuid(), UserId = "otroUsuario" };
            _mockRepo.Setup(r => r.GetByIdAsync(review.Id)).ReturnsAsync(review);

            var result = await _service.DeleteReviewAsync(review.Id, "user1", isAdmin: false, isModerator: true);

            Assert.True(result);
        }

        // ---------- CreateReviewAsync ----------

        [Fact]
        public async Task CreateReviewAsync_CuandoYaExisteReviewDelUsuario_DevuelveFalse()
        {
            var dto = new ReviewCreateDto { GameId = Guid.NewGuid(), Comment = "Buen juego", Rating = 5 };
            _mockRepo.Setup(r => r.ReviewExists("user1", dto.GameId)).ReturnsAsync(true);

            var (success, errorMessage, review) = await _service.CreateReviewAsync(dto, "user1");

            Assert.False(success);
            Assert.Equal("Ya has dejado una reseña para este juego", errorMessage);
            Assert.Null(review);
        }




        [Fact]
        public async Task CreateReviewAsync_CuandoNoExisteReviewPrevia_CreaYDevuelveTrue()
        {
            var dto = new ReviewCreateDto { GameId = Guid.NewGuid(), Comment = "Buen juego", Rating = 5 };
            _mockRepo.Setup(r => r.ReviewExists("user1", dto.GameId)).ReturnsAsync(false);

            var (success, errorMessage, review) = await _service.CreateReviewAsync(dto, "user1");

            Assert.True(success);
            Assert.Equal(string.Empty, errorMessage);
            Assert.NotNull(review);
            Assert.Equal("user1", review.UserId);
            Assert.Equal(dto.Rating, review.Rating);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }


        [Fact]
        public async Task UpdateReviewAsync_CuandoReviewNoExiste_LanzaKeyNotFoundException()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                      .ReturnsAsync((Review)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateReviewAsync(Guid.NewGuid(), new ReviewUpdateDto { Comment = "Nuevo comentario", Rating = 4 }, "user1"));

        }
        [Fact]
        public async Task UpdateReviewAsync_CuandoNoEsDuenio_LanzaUnauthorizedAccessException()
        {
            var review = new Review { Id = Guid.NewGuid(), UserId = "otroUsuario" };
            _mockRepo.Setup(r => r.GetByIdAsync(review.Id)).ReturnsAsync(review);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.UpdateReviewAsync(review.Id, new ReviewUpdateDto { Comment = "Nuevo comentario", Rating = 4 }, "user1"));
        }

        [Fact]
        public async Task UpdateReviewAsync_CuandoEsDuenio_ActualizaYDevuelveReview()
        {
            var review = new Review { Id = Guid.NewGuid(), UserId = "user1", Comment = "Comentario viejo", Rating = 3 };
            _mockRepo.Setup(r => r.GetByIdAsync(review.Id)).ReturnsAsync(review);

            var dto = new ReviewUpdateDto { Comment = "Nuevo comentario", Rating = 4 };
            var result = await _service.UpdateReviewAsync(review.Id, dto, "user1");
            Assert.Equal(dto.Comment, result.Comment);
            Assert.Equal(dto.Rating, result.Rating);

        }

        [Fact]
        public async Task HideReviewAsync_CuandoLaReviewNoExiste_LanzaKeyNotFoundException()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync((Review)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.HideReviewAsync(Guid.NewGuid(), true, true, false));
        }
        [Fact]
        public async Task HideReviewAsync_CuandoNoEsAdminNiModerador_LanzaUnauthorizedAccessExeption()
        {
            var review = new Review { Id = Guid.NewGuid(), UserId = "user1" };
            _mockRepo.Setup(r => r.GetByIdAsync(review.Id)).ReturnsAsync(review);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.HideReviewAsync(review.Id, true, false, false));
        }

        [Fact]
        public async Task HideReviewAsync_CuandoEsAdminPeroNoModerador_CambiaEstadoYDevuelveLaReview()
        {
            var review = new Review { Id = Guid.NewGuid(), UserId = "user1", State = true };
            _mockRepo.Setup(r => r.GetByIdAsync(review.Id)).ReturnsAsync(review);

            var result = await _service.HideReviewAsync(review.Id, false, true, false);
            Assert.False(result.State);
        }
        [Fact]
        public async Task HideReviewAsync_CuandoEsModeradorPeroNoAdmin_CambiaEstadoYDevuelveLaReview()
        {
            var review = new Review { Id = Guid.NewGuid(), UserId = "user1", State = true };
            _mockRepo.Setup(r => r.GetByIdAsync(review.Id)).ReturnsAsync(review);

            var result = await _service.HideReviewAsync(review.Id, false, false, true);
            Assert.False(result.State);
        }
    }
}
