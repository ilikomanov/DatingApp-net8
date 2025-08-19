using System.Collections.Generic;
using System.Threading.Tasks;
using API.Controllers;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using DatingApp.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.Tests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            _mockUserManager = MockUserManager();
            _mockTokenService = new Mock<ITokenService>();
            _mockMapper = new Mock<IMapper>();

            _controller = new AccountController(
                _mockUserManager.Object,
                _mockTokenService.Object,
                _mockMapper.Object
            );
        }

        // Helper to build a mock UserManager<AppUser>
        private static Mock<UserManager<AppUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<AppUser>>();
            return new Mock<UserManager<AppUser>>(
                store.Object, null, null, null, null, null, null, null, null
            );
        }

        [Fact]
        public async Task Register_ReturnsUserDto_WhenRegistrationSuccessful()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Username = "newuser",
                Password = "Pa$$w0rd"
            };

            var appUser = new AppUser
            {
                UserName = "TestUsername",
                KnownAs = "Test",
                Gender = "Male",
                City = "TestCity",
                Country = "TestCountry",
                Photos = new List<Photo>()
            };

            // Mock mapper: RegisterDto -> AppUser
            _mockMapper.Setup(m => m.Map<AppUser>(It.IsAny<RegisterDto>()))
                .Returns(appUser);

            // Use helper to mock async-enabled DbSet<AppUser>
            var users = new List<AppUser>(); // empty list, adjust if needed
            var mockUserDbSet = MockDbSetHelper.CreateMockDbSet(users);

            _mockUserManager.Setup(um => um.Users).Returns(mockUserDbSet.Object);

            // Mock CreateAsync: return success
            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            // Mock token service
            _mockTokenService.Setup(ts => ts.CreateToken(It.IsAny<AppUser>()))
                .ReturnsAsync("fake-token");

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            result.Result.Should().BeNull(); // ActionResult<UserDto> wraps value directly
            result.Value.Should().NotBeNull();
            result.Value.Username.Should().Be("newuser");
            result.Value.Token.Should().Be("fake-token");
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenUserAlreadyExists()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Username = "existinguser",
                Password = "Pa$$w0rd"
            };

            var existingUser = new AppUser
            {
                UserName = "existinguser",
                KnownAs = "Test",
                        Gender = "Male",
                        City = "TestCity",
                        Country = "TestCountry"
            };

            var users = new List<AppUser> { existingUser };
            var mockUserDbSet = MockDbSetHelper.CreateMockDbSet(users);

            _mockUserManager.Setup(um => um.Users).Returns(mockUserDbSet.Object);

            _mockMapper.Setup(m => m.Map<AppUser>(It.IsAny<RegisterDto>()))
                .Returns(new AppUser { UserName = registerDto.Username, KnownAs = "Test",
                        Gender = "Male",
                        City = "TestCity",
                        Country = "TestCountry" });

            // Mock CreateAsync so it returns a failed result
            _mockUserManager
                .Setup(um => um.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Username taken" }));

            // Act
            var actionResult = await _controller.Register(registerDto);

            // Assert
        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = actionResult.Result as BadRequestObjectResult;

        badRequest!.Value.Should().BeEquivalentTo(
            new IdentityError[] { new IdentityError { Description = "Username taken" } }
            );
        }
    }
}
