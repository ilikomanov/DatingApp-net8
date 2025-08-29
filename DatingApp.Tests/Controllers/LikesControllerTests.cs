using System.Security.Claims;
using API.Controllers;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using DatingApp.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DatingApp.Tests.Controllers
{
    public static class TestUserHelpers
    {
        public static ClaimsPrincipal CreateTestUserClaimsPrincipal(int userId) =>
            new(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "mock"));
    }

    public class LikesControllerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
        private readonly Mock<ILikesRepository> _mockLikesRepo = new();
        private readonly LikesController _controller;


        public LikesControllerTests()
        {
            _mockUnitOfWork.Setup(u => u.LikesRepository).Returns(_mockLikesRepo.Object);
            _controller = new LikesController(_mockUnitOfWork.Object);
            
            // default logged-in user id = 5
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = TestUserHelpers.CreateTestUserClaimsPrincipal(5)
                }
            };
        }

        [Fact]
        public async Task ToggleLike_ReturnsBadRequest_WhenLikingSelf()
        {
            var result = await _controller.ToggleLike(5);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("You cannot like yourself", badRequest.Value);

            _mockLikesRepo.Verify(r => r.GetUserLike(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            _mockLikesRepo.Verify(r => r.AddLike(It.IsAny<UserLike>()), Times.Never);
            _mockLikesRepo.Verify(r => r.DeleteLike(It.IsAny<UserLike>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        [Fact]
        public async Task ToggleLike_AddsNewLike_WhenNoneExists_AndSaveSucceeds()
        {
            int targetUserId = 10;

            _mockLikesRepo.Setup(r => r.GetUserLike(5, targetUserId))
                .ReturnsAsync((UserLike?)null);

            _mockUnitOfWork.Setup(u => u.Complete()).ReturnsAsync(true);

            var result = await _controller.ToggleLike(targetUserId);

            Assert.IsType<OkResult>(result);
            _mockLikesRepo.Verify(r => r.AddLike(It.Is<UserLike>(l => l.SourceUserId == 5 && l.TargetUserId == targetUserId)), Times.Once);
            _mockLikesRepo.Verify(r => r.DeleteLike(It.IsAny<UserLike>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        [Fact]
        public async Task ToggleLike_RemovesExistingLike_WhenItExists_AndSaveSucceeds()
        {
            int targetUserId = 10;
            var existingLike = new UserLike { SourceUserId = 5, TargetUserId = targetUserId };

            _mockLikesRepo.Setup(r => r.GetUserLike(5, targetUserId))
                .ReturnsAsync(existingLike);
            _mockUnitOfWork.Setup(u => u.Complete()).ReturnsAsync(true);

            var result = await _controller.ToggleLike(targetUserId);

            Assert.IsType<OkResult>(result);
            _mockLikesRepo.Verify(r => r.DeleteLike(existingLike), Times.Once);
            _mockLikesRepo.Verify(r => r.AddLike(It.IsAny<UserLike>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        [Fact]
        public async Task GetCurrentUserLikeIds_ReturnsOk_WithIds()
        {
            var expectedIds = new List<int> { 2, 3, 4 };
            _mockLikesRepo.Setup(r => r.GetCurrentUserLikeIds(5))
                .ReturnsAsync(expectedIds);

            var result = await _controller.GetCurrentUserLikeIds();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var ids = Assert.IsAssignableFrom<IEnumerable<int>>(okResult.Value);
            Assert.Equal(expectedIds, ids);
        }

        [Fact]
        public async Task GetCurrentUserLikeIds_ReturnsOk_WithEmptyList_WhenNoLikesExist()
        {
            _mockLikesRepo.Setup(r => r.GetCurrentUserLikeIds(5))
                .ReturnsAsync(new List<int>());

            var result = await _controller.GetCurrentUserLikeIds();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var ids = Assert.IsAssignableFrom<IEnumerable<int>>(okResult.Value);
            Assert.Empty(ids);
        }

        [Fact]
        public async Task GetUserLikes_ReturnsOk_WithUsers()
        {
            var likesParams = new LikesParams { Predicate = "liked" };
            var members = new List<MemberDto>
            {
                new MemberDto { Id = 1, Username = "alice" },
                new MemberDto { Id = 2, Username = "bob" }
            };

            var pagedList = new PagedList<MemberDto>(members, members.Count, 1, members.Count);

            _mockLikesRepo.Setup(r => r.GetUserLikes(It.IsAny<LikesParams>()))
                .ReturnsAsync(pagedList);

            var result = await _controller.GetUserLikes(likesParams);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUsers = Assert.IsAssignableFrom<IEnumerable<MemberDto>>(okResult.Value);

            Assert.Equal(2, returnedUsers.Count());
            Assert.Contains(returnedUsers, u => u.Username == "alice");
            Assert.Contains(returnedUsers, u => u.Username == "bob");
        }

        [Fact]
        public async Task GetUserLikes_ReturnsEmptyList_WhenNoUsersLiked()
        {
            var likesParams = new LikesParams { Predicate = "liked" };
            var emptyPagedList = new PagedList<MemberDto>(new List<MemberDto>(), 0, 1, 10);

            _mockLikesRepo.Setup(r => r.GetUserLikes(It.IsAny<LikesParams>()))
                .ReturnsAsync(emptyPagedList);

            var result = await _controller.GetUserLikes(likesParams);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUsers = Assert.IsAssignableFrom<IEnumerable<MemberDto>>(okResult.Value);
            Assert.Empty(returnedUsers);
        }
    }
}
