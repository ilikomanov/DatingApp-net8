using System.Security.Claims;
using API.Controllers;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using DatingApp.Tests.TestHelpers;

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

        [Fact]
        public async Task GetCurrentUserLikeIds_ReturnsOk_WithIds()
        {
            // Arrange
            var expectedIds = new List<int> { 2, 3, 4 };
            _mockLikesRepo.Setup(r => r.GetCurrentUserLikeIds(5))
                .ReturnsAsync(expectedIds);

            // Act
            var result = await _controller.GetCurrentUserLikeIds();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var ids = Assert.IsAssignableFrom<IEnumerable<int>>(okResult.Value);
            Assert.Equal(expectedIds, ids);
        }

        [Fact]
        public async Task GetCurrentUserLikeIds_ReturnsOk_WithIdsV2()
        {
            // Arrange
            var userId = 1;
            var likedIds = new List<int> { 2, 3, 4 };

            // Mock User.GetUserId() extension
            _controller.ControllerContext = new ControllerContext();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext();
            _controller.ControllerContext.HttpContext.User = TestUserHelpers.CreateTestUserClaimsPrincipal(userId);

            _mockLikesRepo.Setup(r => r.GetCurrentUserLikeIds(userId))
                .ReturnsAsync(likedIds);

            // Act
            var result = await _controller.GetCurrentUserLikeIds();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedIds = Assert.IsAssignableFrom<IEnumerable<int>>(okResult.Value);

            Assert.Equal(3, returnedIds.Count());
            Assert.Contains(2, returnedIds);
            Assert.Contains(3, returnedIds);
            Assert.Contains(4, returnedIds);
        }

        [Fact]
        public async Task GetCurrentUserLikeIds_ReturnsOk_WithEmptyList_WhenNoLikesExist()
        {
            // Arrange
            _mockLikesRepo.Setup(r => r.GetCurrentUserLikeIds(5))
                .ReturnsAsync(new List<int>()); // no likes

            // Act
            var result = await _controller.GetCurrentUserLikeIds();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var ids = Assert.IsAssignableFrom<IEnumerable<int>>(okResult.Value);
            Assert.Empty(ids);
        }

        [Fact]
        public async Task GetUserLikes_ReturnsOk_WithEmptyList()
        {
            // Arrange
            var likesParams = new LikesParams { Predicate = "liked" };
            var members = new List<MemberDto>(); // no users

            var pagedList = new PagedList<MemberDto>(
                members, members.Count, 1, members.Count
            );

            _mockLikesRepo.Setup(r => r.GetUserLikes(It.IsAny<LikesParams>()))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetUserLikes(likesParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUsers = Assert.IsAssignableFrom<IEnumerable<MemberDto>>(okResult.Value);

            Assert.Empty(returnedUsers);
        }

        [Fact]
        public async Task GetCurrentUserLikeIds_ReturnsOk_WithEmptyList()
        {
            // Arrange
            var userId = 1;
            var likedIds = new List<int>(); // no likes

            _controller.ControllerContext = new ControllerContext();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext();
            _controller.ControllerContext.HttpContext.User = TestUserHelpers.CreateTestUserClaimsPrincipal(userId);

            _mockLikesRepo.Setup(r => r.GetCurrentUserLikeIds(userId))
                .ReturnsAsync(likedIds);

            // Act
            var result = await _controller.GetCurrentUserLikeIds();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedIds = Assert.IsAssignableFrom<IEnumerable<int>>(okResult.Value);

            Assert.Empty(returnedIds);
        }

        [Fact]
        public async Task GetUserLikes_ReturnsOk_WithUsers()
        {
            // Arrange
            var likesParams = new LikesParams { Predicate = "liked" };
            var members = new List<MemberDto>
            {
                new MemberDto { Id = 1, Username = "alice" },
                new MemberDto { Id = 2, Username = "bob" }
            };

            var pagedList = new PagedList<MemberDto>(
                members, members.Count, 1, members.Count
            );

            _mockLikesRepo.Setup(r => r.GetUserLikes(It.IsAny<LikesParams>()))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetUserLikes(likesParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUsers = Assert.IsAssignableFrom<IEnumerable<MemberDto>>(okResult.Value);

            Assert.Equal(2, returnedUsers.Count());
            Assert.Contains(returnedUsers, u => u.Username == "alice");
            Assert.Contains(returnedUsers, u => u.Username == "bob");
        }

        [Fact]
        public async Task GetUserLikes_ReturnsOk_WhenNoUsers()
        {
            // Arrange
            var likesParams = new LikesParams { Predicate = "liked" };
            var emptyPagedList = new PagedList<MemberDto>(
                new List<MemberDto>(), 0, 1, 10
            );

            _mockLikesRepo.Setup(r => r.GetUserLikes(It.IsAny<LikesParams>()))
                .ReturnsAsync(emptyPagedList);

            // Act
            var result = await _controller.GetUserLikes(likesParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUsers = Assert.IsAssignableFrom<IEnumerable<MemberDto>>(okResult.Value);

            Assert.Empty(returnedUsers);
        }
    }
}
