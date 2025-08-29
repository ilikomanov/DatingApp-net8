using System.Security.Claims;
using API.Controllers;
using API.Entities;
using API.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DatingApp.Tests.Controllers
{
    public class LikesControllerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow = new();
        private readonly Mock<ILikesRepository> _mockLikesRepo = new();
        private readonly LikesController _controller;

        public LikesControllerTests()
        {
            _mockUow.Setup(u => u.LikesRepository).Returns(_mockLikesRepo.Object);

            _controller = new LikesController(_mockUow.Object);

            // default logged-in user id = 5
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "5")
                }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task ToggleLike_ReturnsBadRequest_WhenLikingSelf()
        {
            // Arrange
            var targetUserId = 5; // same as logged-in user id

            // Act
            var result = await _controller.ToggleLike(targetUserId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("You cannot like yourself");

            _mockLikesRepo.Verify(r => r.GetUserLike(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            _mockLikesRepo.Verify(r => r.AddLike(It.IsAny<UserLike>()), Times.Never);
            _mockLikesRepo.Verify(r => r.DeleteLike(It.IsAny<UserLike>()), Times.Never);
            _mockUow.Verify(u => u.Complete(), Times.Never);
        }

        [Fact]
        public async Task ToggleLike_ReturnsBadRequest_WhenSaveFails()
        {
            // Arrange
            var targetUserId = 10;
            _mockLikesRepo.Setup(r => r.GetUserLike(5, targetUserId))
                .ReturnsAsync((UserLike?)null);

            _mockUow.Setup(u => u.Complete()).ReturnsAsync(false);

            // Act
            var result = await _controller.ToggleLike(targetUserId);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Failed to update like", badRequest.Value);

            _mockLikesRepo.Verify(r => r.AddLike(It.IsAny<UserLike>()), Times.Once);
            _mockUow.Verify(u => u.Complete(), Times.Once);
        }

        [Fact]
        public async Task ToggleLike_AddsNewLike_WhenNoneExists_AndSaveSucceeds()
        {
            // Arrange
            var targetUserId = 10; // different from logged-in user (5)

            _mockLikesRepo.Setup(r => r.GetUserLike(5, targetUserId))
                .ReturnsAsync((UserLike)null);

            _mockUow.Setup(u => u.Complete()).ReturnsAsync(true);

            // Act
            var result = await _controller.ToggleLike(targetUserId);

            // Assert
            result.Should().BeOfType<OkResult>();

            _mockLikesRepo.Verify(r => r.AddLike(It.Is<UserLike>(
                l => l.SourceUserId == 5 && l.TargetUserId == targetUserId)), Times.Once);

            _mockLikesRepo.Verify(r => r.DeleteLike(It.IsAny<UserLike>()), Times.Never);
            _mockUow.Verify(u => u.Complete(), Times.Once);
        }

        [Fact]
        public async Task ToggleLike_RemovesExistingLike_WhenItExists_AndSaveSucceeds()
        {
            // Arrange
            var targetUserId = 10;
            var existingLike = new UserLike { SourceUserId = 5, TargetUserId = targetUserId };

            _mockLikesRepo.Setup(r => r.GetUserLike(5, targetUserId))
                .ReturnsAsync(existingLike);

            _mockUow.Setup(u => u.Complete()).ReturnsAsync(true);

            // Act
            var result = await _controller.ToggleLike(targetUserId);

            // Assert
            result.Should().BeOfType<OkResult>();

            _mockLikesRepo.Verify(r => r.DeleteLike(existingLike), Times.Once);
            _mockLikesRepo.Verify(r => r.AddLike(It.IsAny<UserLike>()), Times.Never);
            _mockUow.Verify(u => u.Complete(), Times.Once);
        }
    }
}
