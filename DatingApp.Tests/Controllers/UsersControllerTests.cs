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
using CloudinaryDotNet.Actions;


namespace DatingApp.Tests.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IPhotoService> _mockPhotoService;
        private UsersController _controller;

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
        public async Task GetUser_ReturnsNull_WhenUserDoesNotExist()
        {
            // Arrange
            var username = "nonexistent";
            var mockHttpContext = new Mock<HttpContext>();
            var mockClaimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "currentUser")
                }, "mock")
            );
            mockHttpContext.Setup(c => c.User).Returns(mockClaimsPrincipal);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetMemberAsync(username, false))
                .ReturnsAsync((MemberDto)null!);

            // Act
            var result = await _controller.GetUser(username);

            // Assert
            result.Value.Should().BeNull();
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
        public async Task GetUser_CallsRepositoryWithIsCurrentUserTrue_WhenRequestingSelf()
        {
            // Arrange
            var username = "currentUser";
            var mockHttpContext = new Mock<HttpContext>();
            var mockClaimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username)
                }, "mock")
            );
            mockHttpContext.Setup(c => c.User).Returns(mockClaimsPrincipal);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var expectedMember = new MemberDto { Username = username };
            _mockUnitOfWork.Setup(u => u.UserRepository.GetMemberAsync(username, true))
                .ReturnsAsync(expectedMember);

            // Act
            var result = await _controller.GetUser(username);

            // Assert
            result.Value.Should().Be(expectedMember);
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
        public async Task GetUser_CallsRepositoryWithIsCurrentUserFalse_WhenRequestingAnotherUser()
        {
            // Arrange
            var currentUsername = "currentUser";
            var requestedUsername = "otherUser";

            var mockHttpContext = new Mock<HttpContext>();
            var mockClaimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, currentUsername)
                }, "mock")
            );
            mockHttpContext.Setup(c => c.User).Returns(mockClaimsPrincipal);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var expectedMember = new MemberDto { Username = requestedUsername };
            _mockUnitOfWork.Setup(u => u.UserRepository.GetMemberAsync(requestedUsername, false))
                .ReturnsAsync(expectedMember);

            // Act
            var result = await _controller.GetUser(requestedUsername);

            // Assert
            result.Value.Should().Be(expectedMember);
            _mockUnitOfWork.Verify(u => u.UserRepository.GetMemberAsync(requestedUsername, false), Times.Once);
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
        public async Task UpdateUser_ReturnsBadRequest_WhenUserNotFoundV2()
        {
            // Arrange
            var currentUsername = "testuser";

            var mockHttpContext = new Mock<HttpContext>();
            var mockClaimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, currentUsername)
                }, "mock")
            );
            mockHttpContext.Setup(c => c.User).Returns(mockClaimsPrincipal);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(currentUsername))
                .ReturnsAsync((AppUser)null!);

            var updateDto = new MemberUpdateDto
            {
                Introduction = "New intro"
            };

            // Act
            var result = await _controller.UpdateUser(updateDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Could not find user");

            _mockUnitOfWork.Verify(u => u.UserRepository.GetUserByUsernameAsync(currentUsername), Times.Once);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenUserNotFoundV3()
        {
            // Arrange
            var username = "nonexistentuser";
            var updateDto = new MemberUpdateDto
            {
                Introduction = "Hello world",
                LookingFor = "Friendship",
                Interests = "Coding, Reading",
                City = "TestCity",
                Country = "TestCountry"
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(username))
                .ReturnsAsync((AppUser?)null); // User not found

            var mockHttpContext = new Mock<HttpContext>();
            var mockClaimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username)
                }, "mock")
            );
            mockHttpContext.Setup(c => c.User).Returns(mockClaimsPrincipal);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = await _controller.UpdateUser(updateDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("Could not find user");

            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never); // Complete should not be called
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

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            var username = "testuser";
            var updateDto = new MemberUpdateDto { Introduction = "Updated intro" };
            var user = new AppUser
            {
                UserName = "testuser",
                KnownAs = "Test",
                Gender = "Male",
                City = "TestCity",
                Country = "TestCountry"
            };

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
                .ReturnsAsync(user);

            _mockUnitOfWork.Setup(u => u.Complete())
                .ReturnsAsync(false); // Simulate failure

            // Act
            var result = await _controller.UpdateUser(updateDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("Failed to update the user");
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenUpdateFailsV2()
        {
            // Arrange
            var currentUsername = "testuser";

            var mockHttpContext = new Mock<HttpContext>();
            var mockClaimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, currentUsername)
                }, "mock")
            );
            mockHttpContext.Setup(c => c.User).Returns(mockClaimsPrincipal);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var appUser = new AppUser
            {
                UserName = currentUsername,
                KnownAs = "Test",
                Gender = "Male",
                City = "TestCity",
                Country = "TestCountry"
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(currentUsername))
                .ReturnsAsync(appUser);

            // Ensure mapping is called (can be ignored in test)
            var updateDto = new MemberUpdateDto { Introduction = "New intro" };

            // Simulate failure
            _mockUnitOfWork.Setup(u => u.Complete()).ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateUser(updateDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Failed to update the user");

            _mockUnitOfWork.Verify(u => u.UserRepository.GetUserByUsernameAsync(currentUsername), Times.Once);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenUpdateFailsV3()
        {
            // Arrange
            var username = "testuser";
            var updateDto = new MemberUpdateDto
            {
                Introduction = "Hello world",
                LookingFor = "Friendship",
                Interests = "Coding, Reading",
                City = "TestCity",
                Country = "TestCountry"
            };

            var appUser = new AppUser
            {
                UserName = username,
                Introduction = "Old intro",
                LookingFor = "Old looking for",
                Interests = "Old interests",
                City = "OldCity",
                Country = "OldCountry",
                KnownAs = "Old Test",
                Gender = "Male"
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(username))
                .ReturnsAsync(appUser);

            _mockUnitOfWork.Setup(u => u.Complete())
                .ReturnsAsync(false); // Simulate failure

            var mockHttpContext = new Mock<HttpContext>();
            var mockClaimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username)
                }, "mock")
            );
            mockHttpContext.Setup(c => c.User).Returns(mockClaimsPrincipal);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = await _controller.UpdateUser(updateDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("Failed to update the user");

            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once); // Complete was called
        }

        [Fact]
        public async Task UpdateUser_ReturnsNoContent_WhenUpdateIsSuccessfulV2()
        {
            // Arrange
            var username = "testuser";
            var updateDto = new MemberUpdateDto { LookingFor = "New expectations" };
            var user = new AppUser
            {
                UserName = "testuser",
                KnownAs = "Test",
                Gender = "Male",
                City = "TestCity",
                Country = "TestCountry"
            };

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
                .ReturnsAsync(user);

            _mockUnitOfWork.Setup(u => u.Complete())
                .ReturnsAsync(true); // Simulate success

            // Act
            var result = await _controller.UpdateUser(updateDto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task UpdateUser_ReturnsNoContent_WhenUpdateIsSuccessfulV3()
        {
            // Arrange
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<MemberUpdateDto, AppUser>();
            });
            var mapper = config.CreateMapper();

            var username = "testuser";
            var updateDto = new MemberUpdateDto
            {
                Introduction = "Hello world",
                LookingFor = "Friendship",
                Interests = "Coding, Reading",
                City = "TestCity",
                Country = "TestCountry"
            };

            var appUser = new AppUser
            {
                UserName = username,
                Introduction = "Old intro",
                LookingFor = "Old looking for",
                Interests = "Old interests",
                City = "OldCity",
                Country = "OldCountry",
                KnownAs = "Old Test",
                Gender = "Male",
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(username))
                .ReturnsAsync(appUser);

            _mockUnitOfWork.Setup(u => u.Complete())
                .ReturnsAsync(true);

            var mockHttpContext = new Mock<HttpContext>();
            var mockClaimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username)
                }, "mock")
            );
            mockHttpContext.Setup(c => c.User).Returns(mockClaimsPrincipal);

            _controller = new UsersController(_mockUnitOfWork.Object, mapper, _mockPhotoService.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = await _controller.UpdateUser(updateDto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            appUser.Introduction.Should().Be(updateDto.Introduction);
            appUser.LookingFor.Should().Be(updateDto.LookingFor);
            appUser.Interests.Should().Be(updateDto.Interests);
            appUser.City.Should().Be(updateDto.City);
            appUser.Country.Should().Be(updateDto.Country);

            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ReturnsNoContent_WhenUpdateSucceeds()
        {
            // Arrange
            var currentUsername = "testuser";

            var mockHttpContext = new Mock<HttpContext>();
            var mockClaimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, currentUsername)
                }, "mock")
            );
            mockHttpContext.Setup(c => c.User).Returns(mockClaimsPrincipal);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var appUser = new AppUser
            {
                UserName = currentUsername,
                KnownAs = "Test",
                Gender = "Male",
                City = "TestCity",
                Country = "TestCountry"
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(currentUsername))
                .ReturnsAsync(appUser);

            var updateDto = new MemberUpdateDto { Introduction = "Updated intro" };

            // Simulate successful save
            _mockUnitOfWork.Setup(u => u.Complete()).ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateUser(updateDto);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            _mockUnitOfWork.Verify(u => u.UserRepository.GetUserByUsernameAsync(currentUsername), Times.Once);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        [Fact]
        public async Task AddPhoto_ReturnsBadRequest_WhenPhotoServiceFails()
        {
            // Arrange
            var mockFormFile = new Mock<IFormFile>();

            // Mock the logged-in user context
            var currentUsername = "testuser";
            var mockHttpContext = new Mock<HttpContext>();
            var mockClaimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, currentUsername)
                }, "mock")
            );

            mockHttpContext.Setup(c => c.User).Returns(mockClaimsPrincipal);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Mock repo to return a valid user
            var appUser = new AppUser
            {
                UserName = "testuser",
                KnownAs = "Test",
                Gender = "Male",
                City = "TestCity",
                Country = "TestCountry",
                Photos = new List<Photo>()
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(currentUsername))
                .ReturnsAsync(appUser);

            // Mock photo service to return a result with an Error
            _mockPhotoService.Setup(p => p.AddPhotoAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new ImageUploadResult
                {
                    Error = new Error { Message = "Upload failed" }
                });

            // Act
            var result = await _controller.AddPhoto(mockFormFile.Object);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task AddPhoto_ReturnsCreatedAtAction_WhenSuccessful()
        {
            // Arrange
            var mockFormFile = new Mock<IFormFile>();

            var currentUsername = "testuser";
            var mockHttpContext = new Mock<HttpContext>();
            var mockClaimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, currentUsername)
                }, "mock")
            );

            mockHttpContext.Setup(c => c.User).Returns(mockClaimsPrincipal);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var appUser = new AppUser
            {
                UserName = "testuser",
                KnownAs = "Test",
                Gender = "Male",
                City = "TestCity",
                Country = "TestCountry",
                Photos = new List<Photo>()
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(currentUsername))
                .ReturnsAsync(appUser);

            // Mock successful photo upload
            _mockPhotoService.Setup(p => p.AddPhotoAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new ImageUploadResult
                {
                    SecureUrl = new Uri("http://test.com/photo.jpg"),
                    PublicId = "public123"
                });

            _mockUnitOfWork.Setup(u => u.Complete()).ReturnsAsync(true);

            // Mock mapper so PhotoDto is not null
            _mockMapper.Setup(m => m.Map<PhotoDto>(It.IsAny<Photo>()))
                .Returns((Photo p) => new PhotoDto
                {
                    Id = p.Id,
                    Url = p.Url,
                    IsMain = p.IsMain,
                    IsApproved = p.IsApproved
                });

            // Act
            var result = await _controller.AddPhoto(mockFormFile.Object);

            // Assert
            result.Result.Should().BeOfType<CreatedAtActionResult>();

            var createdResult = result.Result as CreatedAtActionResult;
            createdResult!.ActionName.Should().Be(nameof(_controller.GetUser));
            createdResult.Value.Should().BeOfType<PhotoDto>();

            var photoDto = createdResult.Value as PhotoDto;
            photoDto!.Url.Should().Be("http://test.com/photo.jpg");
        }

        [Fact]
        public async Task AddPhoto_ReturnsCreatedAtAction_WhenPhotoAddedSuccessfully()
        {
            // Arrange
            var username = "testuser";
            var appUser = new AppUser
            {
                UserName = username,
                KnownAs = "Test",
                        Gender = "Male",
                        City = "TestCity",
                        Country = "TestCountry",
                Photos = new List<Photo>()
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(username))
                .ReturnsAsync(appUser);

            var uploadResult = new ImageUploadResult
            {
                SecureUrl = new Uri("http://test.com/photo.jpg"),
                PublicId = "public123"
            };

            _mockPhotoService.Setup(p => p.AddPhotoAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(uploadResult);

            _mockUnitOfWork.Setup(u => u.Complete())
                .ReturnsAsync(true);

            var mockHttpContext = new Mock<HttpContext>();
            var mockClaimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username)
                }, "mock")
            );
            mockHttpContext.Setup(c => c.User).Returns(mockClaimsPrincipal);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var fileMock = new Mock<IFormFile>();
            var content = "fake image content";
            var fileName = "test.jpg";
            var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(ms.Length);

            // Act
            var result = await _controller.AddPhoto(fileMock.Object!);

            // Assert
            var createdAtAction = result.Result as CreatedAtActionResult;
            createdAtAction.Should().NotBeNull();
            createdAtAction!.ActionName.Should().Be(nameof(_controller.GetUser));
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        [Fact]
        public async Task AddPhoto_ReturnsCreatedAtRoute_WhenPhotoAddedSuccessfully()
        {
            // Arrange
            var currentUsername = "testuser";
            var mockHttpContext = new Mock<HttpContext>();
            var mockClaimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, currentUsername)
                }, "mock")
            );
            mockHttpContext.Setup(c => c.User).Returns(mockClaimsPrincipal);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var appUser = new AppUser
            {
                UserName = currentUsername,
                KnownAs = "Test",
                Gender = "Male",
                City = "TestCity",
                Country = "TestCountry",
                Photos = new List<Photo>()
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(currentUsername))
                .ReturnsAsync(appUser);

            var fileMock = new Mock<IFormFile>();

            // Mock successful photo upload
            _mockPhotoService.Setup(p => p.AddPhotoAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new ImageUploadResult
                {
                    SecureUrl = new Uri("http://photo.url"),
                    PublicId = "public123"
                });

            _mockUnitOfWork.Setup(u => u.Complete()).ReturnsAsync(true);

            // Act
            var result = await _controller.AddPhoto(fileMock.Object);

            // Assert
            var createdAtActionResult = result.Result as CreatedAtActionResult;
            createdAtActionResult.Should().NotBeNull();
            createdAtActionResult.ActionName.Should().Be(nameof(_controller.GetUser));
            createdAtActionResult.RouteValues["username"].Should().Be(currentUsername);

            appUser.Photos.Should().ContainSingle();
            appUser.Photos.First().Url.Should().Be("http://photo.url/"); // matches mocked SecureUrl

            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
            _mockPhotoService.Verify(s => s.AddPhotoAsync(fileMock.Object), Times.Once);
        }

        [Fact]
        public async Task AddPhoto_ReturnsBadRequest_WhenUnitOfWorkFails()
        {
            // Arrange
            var mockFormFile = new Mock<IFormFile>();

            var currentUsername = "testuser";
            var mockHttpContext = new Mock<HttpContext>();
            var mockClaimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, currentUsername)
                }, "mock")
            );

            mockHttpContext.Setup(c => c.User).Returns(mockClaimsPrincipal);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var appUser = new AppUser
            {
                UserName = "testuser",
                KnownAs = "Test",
                Gender = "Male",
                City = "TestCity",
                Country = "TestCountry",
                Photos = new List<Photo>()
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(currentUsername))
                .ReturnsAsync(appUser);

            _mockPhotoService.Setup(p => p.AddPhotoAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new ImageUploadResult
                {
                    SecureUrl = new Uri("http://test.com/photo.jpg"),
                    PublicId = "public123"
                });

            // Simulate failure in saving changes
            _mockUnitOfWork.Setup(u => u.Complete()).ReturnsAsync(false);

            // Act
            var result = await _controller.AddPhoto(mockFormFile.Object);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result.Result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("Problem adding photo");
        }

        [Fact]
        public async Task AddPhoto_ReturnsBadRequest_WhenPhotoServiceFailsV2()
        {
            // Arrange
            var mockFormFile = new Mock<IFormFile>();

            var currentUsername = "testuser";
            var mockHttpContext = new Mock<HttpContext>();
            var mockClaimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, currentUsername)
                }, "mock")
            );

            mockHttpContext.Setup(c => c.User).Returns(mockClaimsPrincipal);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var appUser = new AppUser
            {
                UserName = "testuser",
                KnownAs = "Test",
                Gender = "Male",
                City = "TestCity",
                Country = "TestCountry",
                Photos = new List<Photo>()
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(currentUsername))
                .ReturnsAsync(appUser);

            // Simulate a failure from the photo service
            _mockPhotoService.Setup(p => p.AddPhotoAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new ImageUploadResult
                {
                    Error = new Error { Message = "Upload failed" }
                });

            // Act
            var result = await _controller.AddPhoto(mockFormFile.Object);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result.Result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("Upload failed");
        }

        [Fact]
        public async Task AddPhoto_ReturnsBadRequest_WhenPhotoServiceFailsV3()
        {
            // Arrange
            var username = "testuser";
            var fileMock = new Mock<IFormFile>();

            var appUser = new AppUser
            {
                UserName = username,
                KnownAs = "Test",
                Gender = "Male",
                City = "TestCity",
                Country = "TestCountry",
                Photos = new List<Photo>()
            };

            // Simulate logged-in user
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Name, username) }, "mock"))
            };
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(username))
                .ReturnsAsync(appUser);

            // Mock photo service failure
            var uploadResult = new ImageUploadResult
            {
                Error = new Error { Message = "Upload failed" }
            };

            _mockPhotoService.Setup(p => p.AddPhotoAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(uploadResult);

            // Act
            var result = await _controller.AddPhoto(fileMock.Object);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = (BadRequestObjectResult)result.Result!;
            badRequest.Value.Should().Be("Upload failed");

            // Photo should not be added
            appUser.Photos.Should().BeEmpty();
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        [Fact]
        public async Task AddPhoto_ReturnsBadRequest_WhenUserNotFound()
        {
            // Arrange
            var username = "testuser";
            var fileMock = new Mock<IFormFile>();

            // Simulate logged-in user
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Name, username) }, "mock"))
            };
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // User repo returns null -> user not found
            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(username))
                .ReturnsAsync((AppUser?)null);

            // Act
            var result = await _controller.AddPhoto(fileMock.Object);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = (BadRequestObjectResult)result.Result!;
            badRequest.Value.Should().Be("Cannot update user");

            // Photo service should not be called
            _mockPhotoService.Verify(p => p.AddPhotoAsync(It.IsAny<IFormFile>()), Times.Never);
        }

        [Fact]
        public async Task AddPhoto_ReturnsBadRequest_WhenUserNotFoundV2()
        {
            // Arrange
            var username = "testuser";

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(username))
                .ReturnsAsync((AppUser)null); // simulate no user found

            var mockHttpContext = new Mock<HttpContext>();
            var mockClaimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username)
                }, "mock")
            );
            mockHttpContext.Setup(c => c.User).Returns(mockClaimsPrincipal);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var fileMock = new Mock<IFormFile>();

            // Act
            var result = await _controller.AddPhoto(fileMock.Object);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result.Result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("Cannot update user");

            _mockUnitOfWork.Verify(u => u.UserRepository.GetUserByUsernameAsync(username), Times.Once);
            _mockPhotoService.Verify(p => p.AddPhotoAsync(It.IsAny<IFormFile>()), Times.Never);
        }

        [Fact]
        public async Task AddPhoto_ReturnsBadRequest_WhenCompleteFails()
        {
            // Arrange
            var username = "testuser";
            var appUser = new AppUser
            {
                UserName = username,
                KnownAs = "Test",
                Gender = "Male",
                City = "TestCity",
                Country = "TestCountry",
                Photos = new List<Photo>()
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(username))
                .ReturnsAsync(appUser);

            var mockHttpContext = new Mock<HttpContext>();
            var mockClaimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username)
                }, "mock")
            );
            mockHttpContext.Setup(c => c.User).Returns(mockClaimsPrincipal);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var fileMock = new Mock<IFormFile>();

            _mockPhotoService.Setup(p => p.AddPhotoAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new ImageUploadResult
                {
                    SecureUrl = new Uri("http://photo.url"),
                    PublicId = "publicId123"
                });

            _mockUnitOfWork.Setup(u => u.Complete()).ReturnsAsync(false); // simulate failure

            // Act
            var result = await _controller.AddPhoto(fileMock.Object);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result.Result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("Problem adding photo");

            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        [Fact]
        public async Task DeletePhoto_ReturnsNoContent_WhenPhotoDeletedSuccessfully()
        {
            // Arrange
            var username = "testuser";
            var photoId = 1;

            var appUser = new AppUser
            {
                UserName = username,
                KnownAs = "Test",
                Gender = "Male",
                City = "TestCity",
                Country = "TestCountry",
                Photos = new List<Photo>
                {
                    new Photo
                    {
                        Id = photoId,
                        Url = "http://photo.url",
                        IsMain = false,
                        PublicId = "publicId123"
                    }
                }
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(username))
                .ReturnsAsync(appUser);

            // **Important**: mock GetPhotoById to avoid NullReferenceException
            _mockUnitOfWork.Setup(u => u.PhotoRepository.GetPhotoById(photoId))
                .ReturnsAsync(appUser.Photos.First());

            _mockPhotoService.Setup(p => p.DeletePhotoAsync("publicId123"))
                .ReturnsAsync(new DeletionResult { Result = "ok" });

            _mockUnitOfWork.Setup(u => u.Complete())
                .ReturnsAsync(true);

            var mockHttpContext = new Mock<HttpContext>();
            var mockClaimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username)
                }, "mock")
            );
            mockHttpContext.Setup(c => c.User).Returns(mockClaimsPrincipal);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = await _controller.DeletePhoto(photoId);

            // Assert
            result.Should().BeOfType<OkResult>();
            appUser.Photos.Should().BeEmpty();
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }
    }
}
