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
    }
}
