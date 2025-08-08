using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Controllers;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DatingApp.Tests.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IPhotoService> _mockPhotoService;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockPhotoService = new Mock<IPhotoService>();

            _controller = new UsersController(_mockUnitOfWork.Object, _mockMapper.Object, _mockPhotoService.Object);

            // Simulate a logged-in user for User.GetUsername()
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "testuser")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetUsers_ReturnsOkResult_WithPagedMembers()
        {
            // Arrange
            var userParams = new UserParams();

            var members = new List<MemberDto>
            {
                new MemberDto { Username = "user1" }
            };

            var pagedList = new PagedList<MemberDto>(
                items: members,
                count: 1,
                pageNumber: 1,
                pageSize: 10
            );

            _mockUnitOfWork.Setup(u => u.UserRepository.GetMembersAsync(It.IsAny<UserParams>()))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetUsers(userParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsAssignableFrom<IEnumerable<MemberDto>>(okResult.Value);
            returnValue.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetUser_ReturnsMemberDto_WhenUserExists()
        {
            // Arange
            var username = "testuser";
            var member = new MemberDto { Username = username };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetMemberAsync(username, true))
                .ReturnsAsync(member);

            // Act
            var result = await _controller.GetUser(username);

            // Assert
            result.Value.Should().NotBeNull();
            result.Value.Username.Should().Be(username);
        }

        [Fact]
        public async Task GetUsers_ReturnsEmptyList_WhenNoUsersFound()
        {
            // Arrange
            var pagedList = new PagedList<MemberDto>(
                items: new List<MemberDto>(),
                count: 0,
                pageNumber: 1,
                pageSize: 10
            );

            _mockUnitOfWork.Setup(u => u.UserRepository.GetMembersAsync(It.IsAny<UserParams>()))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetUsers(new UserParams());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsAssignableFrom<IEnumerable<MemberDto>>(okResult.Value);
            returnValue.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUsers_ReturnsEmpty_WhenNoUsersFound()
        {
            // Arrange
            var userParams = new UserParams();
            var emptyList = new List<MemberDto>();
            var pagedList = new PagedList<MemberDto>(
                items: emptyList,
                count: 0,
                pageNumber: 1,
                pageSize: 10
            );

            _mockUnitOfWork.Setup(u => u.UserRepository.GetMembersAsync(It.IsAny<UserParams>()))
               .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetUsers(userParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsAssignableFrom<IEnumerable<MemberDto>>(okResult.Value);
            returnValue.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var username = "nonexistentuser";

            _mockUnitOfWork.Setup(u => u.UserRepository.GetMemberAsync(username, It.IsAny<bool>()))
                .ReturnsAsync((MemberDto)null);

            // Act
            var result = await _controller.GetUser(username);

            // Assert
            result.Value.Should().BeNull();
        }

        [Fact]
        public async Task GetUser_CallsRepoWithIsCurrentUserTrue_WhenUsernameMatchesCurrentUser()
        {
            // Arrange
            var username = "testuser";
            var member = new MemberDto { Username = username };

            var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetMemberAsync(username, true))
                .ReturnsAsync(member);

            // Act
            var result = await _controller.GetUser(username);

            // Assert
            result.Value.Should().NotBeNull();
            result.Value!.Username.Should().Be(username);
        }

        [Fact]
        public async Task GetUser_CallsRepoWithIsCurrentUserFalse_WhenUsernameDoesNotMatchCurrentUser()
        {
            // Arrange
            var requestedUsername = "otheruser";
            var currentUsername = "testuser";
            var member = new MemberDto { Username = requestedUsername };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, currentUsername)
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.User).Returns(principal);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetMemberAsync(requestedUsername, false))
                .ReturnsAsync(member);

            // Act
            var result = await _controller.GetUser(requestedUsername);

            // Assert
            result.Value.Should().NotBeNull();
            result.Value!.Username.Should().Be(requestedUsername);
        }


        [Fact]
        public async Task UpdateUser_ReturnsNoContent_WhenUpdateIsSuccessful()
        {
            // Arrange
            var updateDto = new MemberUpdateDto { Introduction = "Hello!" };
            var user = new AppUser
            {
                UserName = "testuser",
                KnownAs = "Test",
                Gender = "Male",
                City = "TestCity",
                Country = "TestCountry"
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync("testuser"))
                .ReturnsAsync(user);

            _mockUnitOfWork.Setup(u => u.Complete())
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateUser(updateDto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenUserNotFound()
        {
            // Arrange
            var updateDto = new MemberUpdateDto();

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync("testuser"))
                .ReturnsAsync((AppUser)null);

            // Act
            var result = await _controller.UpdateUser(updateDto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            badRequest.Value.Should().Be("Could not find user");
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenSaveFails()
        {
            // Arrange
            var updateDto = new MemberUpdateDto { Introduction = "Update" };
            var user = new AppUser
            {
                UserName = "testuser",
                KnownAs = "Test",
                Gender = "Male",
                City = "TestCity",
                Country = "TestCountry"
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync("testuser"))
                .ReturnsAsync(user);

            _mockUnitOfWork.Setup(u => u.Complete())
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateUser(updateDto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            badRequest.Value.Should().Be("Failed to update the user");
        }
        
        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenUserDoesNotExist()
        {
            // Arrange
            var username = "testuser";
            var updateDto = new MemberUpdateDto();

            var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };
            var identity = new ClaimsIdentity(claims);
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.User).Returns(claimsPrincipal);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(username))
                .ReturnsAsync((AppUser?)null); // Simulate user not found

            // Act
            var result = await _controller.UpdateUser(updateDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("Could not find user");
        }
    }
}
