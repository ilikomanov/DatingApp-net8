using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Controllers;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using System.Diagnostics.CodeAnalysis;
using DatingApp.Tests.TestHelpers;

namespace DatingApp.Tests.Controllers
{
    public class AdminControllerTests
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IPhotoService> _mockPhotoService;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            _mockUserManager = new Mock<UserManager<AppUser>>(
                Mock.Of<IUserStore<AppUser>>(), null, null, null, null, null, null, null, null);

            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockPhotoService = new Mock<IPhotoService>();

            _controller = new AdminController(_mockUserManager.Object, _mockUnitOfWork.Object, _mockPhotoService.Object);

            // Mock logged-in admin user
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "admin")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetUsersWithRoles_ReturnsOkWithUsers()
        {
            // Arrange: use in-memory list
            var users = new List<AppUser>
        {
            new AppUser
            {
                Id = 1,
                UserName = "alice",
                Gender = "female",
                KnownAs = "Alice",
                City = "Paris",
                Country = "France",
                UserRoles = new List<AppUserRole>
                {
                    new AppUserRole { Role = new AppRole { Name = "Admin" } }
                }
            },
            new AppUser
            {
                Id = 2,
                UserName = "bob",
                Gender = "male",
                KnownAs = "Bob",
                City = "London",
                Country = "UK",
                UserRoles = new List<AppUserRole>
                {
                    new AppUserRole { Role = new AppRole { Name = "Member" } }
                }
            }
        };

            // Return plain IQueryable (no async needed)
            _mockUserManager.Setup(um => um.Users).Returns(new TestAsyncEnumerable<AppUser>(users));

            // Act: temporarily override ToListAsync() with ToList() using LINQ in test
            var result = await _controller.GetUsersWithRoles();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUsers = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Equal(2, returnedUsers.Count());
        }

        [Fact]
        public async Task EditRoles_ReturnsOk_WhenRolesAreUpdated()
        {
            // Arrange
            var user = new AppUser
            {
                Id = 1,
                UserName = "alice",
                Gender = "female",
                KnownAs = "Alice",
                City = "Paris",
                Country = "France"
            };

            var existingRoles = new List<string> { "Member" };
            var updatedRoles = new List<string> { "Admin" };

            _mockUserManager.Setup(um => um.FindByNameAsync("alice"))
                .ReturnsAsync(user);

            _mockUserManager.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(existingRoles);

            _mockUserManager.Setup(um => um.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(um => um.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(updatedRoles);

            // Act
            var result = await _controller.EditRoles("alice", "Admin");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var roles = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
            Assert.Single(roles);
            Assert.Equal("Admin", roles.First());
        }

        [Fact]
        public async Task EditRoles_ReturnsBadRequest_WhenRolesEmpty()
        {
            // Act
            var result = await _controller.EditRoles("alice", "");

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("You must select at least one role", badRequest.Value);
        }

        [Fact]
        public async Task EditRoles_ReturnsBadRequest_WhenUserNotFound()
        {
            // Arrange
            _mockUserManager.Setup(um => um.FindByNameAsync("unknown"))
                .ReturnsAsync((AppUser)null);

            // Act
            var result = await _controller.EditRoles("unknown", "Admin");

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("User not found", badRequest.Value);
        }

        [Fact]
        public async Task EditRoles_ReturnsBadRequest_WhenNoRolesProvided()
        {
            
            // Act
            var result = await _controller.EditRoles("alice", "");

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("You must select at least one role", badRequest.Value);
        }
    }
}
