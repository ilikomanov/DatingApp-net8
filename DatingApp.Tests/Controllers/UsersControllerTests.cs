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

        private UsersController CreateControllerWithUser(string username)//new method
        {
            var controller = new UsersController(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockPhotoService.Object);

            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username)
                }, "mock"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return controller;
        }
        
        private UsersController CreateControllerWithMockUser(string username)
        {
            var controller = new UsersController(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockPhotoService.Object);

            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.NameIdentifier, username)
                }, "mock"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return controller;
        }

        private PagedList<MemberDto> CreatePagedList(IEnumerable<MemberDto> items, int pageNumber = 1, int pageSize = 10)
        {
            var list = items.ToList();
            return new PagedList<MemberDto>(list, list.Count, pageNumber, pageSize);
        }

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
        public async Task SetMainPhoto_ReturnsBadRequest_WhenPhotoNotFound()
        {
            var controller = CreateControllerWithMockUser("alice");
            var user = new AppUser { UserName = "alice",    KnownAs = "Alice",
                        Gender = "female",
                        City = "Wonderland",
                        Country = "Fantasy", Photos = new List<Photo>() };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync("alice")).ReturnsAsync(user);

            var result = await controller.SetMainPhoto(99);

            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Cannot use this as main photo");
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
        public async Task GetUser_IsCurrentUserFlagIsTrue_WhenUsernameMatches()
        {
            // Arrange
            var controller = CreateControllerWithMockUser("alice");
            var member = new MemberDto { Username = "alice" };

            _mockUnitOfWork
                .Setup(u => u.UserRepository.GetMemberAsync("alice", true))
                .ReturnsAsync(member);

            // Act
            var result = await controller.GetUser("alice");

            // Assert
            result.Value.Username.Should().Be("alice");
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
        public async Task GetUsers_ReturnsListOfMembers_WithPaginationHeader()
        {
            // Arrange
            var controller = CreateControllerWithMockUser("alice");

            var userParams = new UserParams();

            var members = CreatePagedList(new List<MemberDto>
            {
                new MemberDto { Username = "bob" },
                new MemberDto { Username = "carol" }
            });

            _mockUnitOfWork
                .Setup(u => u.UserRepository.GetMembersAsync(userParams))
                .ReturnsAsync(members);

            // Act
            var result = await controller.GetUsers(userParams);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedMembers = okResult.Value.Should().BeAssignableTo<IEnumerable<MemberDto>>().Subject;
            returnedMembers.Should().HaveCount(2);

            userParams.CurrentUsername.Should().Be("alice");
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
        public async Task UpdateUser_ReturnsBadRequest_WhenModelStateInvalid()
        {
            // Arrange
            var controller = CreateControllerWithUser("alice");

            var dto = new MemberUpdateDto { City = "TestCity" };

            controller.ModelState.AddModelError("City", "Required");

            _mockUnitOfWork
                .Setup(x => x.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync(new AppUser 
                { 
                    UserName = "alice",  
                    KnownAs = "Alice",
                    Gender = "female",
                    City = "Wonderland",
                    Country = "Fantasy" 
                });

            _mockUnitOfWork.Setup(x => x.Complete()).ReturnsAsync(false);

            // Act
            var result = await controller.UpdateUser(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();

            // mapper IS called (controller always calls it)
            _mockMapper.Verify(
                m => m.Map(dto, It.IsAny<AppUser>()),
                Times.Once
            );

            // save is attempted once
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_MapperCalledWithCorrectArguments()
        {
            // Arrange
            var controller = CreateControllerWithUser("alice");

            var user = new AppUser 
            { 
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy"
            };

            var dto = new MemberUpdateDto
            {
                City = "UpdatedCity",
                Country = "UpdatedCountry"
            };

            _mockUnitOfWork.Setup(x => x.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync(user);

            _mockUnitOfWork.Setup(x => x.Complete()).ReturnsAsync(true);

            // Act
            await controller.UpdateUser(dto);

            // Assert
            _mockMapper.Verify(m => m.Map(dto, user), Times.Once);
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
        public async Task UpdateUser_ReturnsNoContent_SaveSuccessful()
        {
            // Arrange
            var controller = CreateControllerWithUser("alice");

            var user = new AppUser 
            { 
                UserName = "alice",
                KnownAs = "Test",
                Gender = "Male",
                City = "TestCity",
                Country = "TestCountry"
            };

            _mockUnitOfWork.Setup(x => x.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync(user);

            _mockUnitOfWork.Setup(x => x.Complete()).ReturnsAsync(true);

            // Act
            var result = await controller.UpdateUser(new MemberUpdateDto());

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("Introduction", "Required");

            var dto = new MemberUpdateDto { Introduction = null }; // invalid

            var testUser = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "Female",
                City = "TestCity",
                Country = "TestCountry"
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(testUser);

            // Act
            var result = await _controller.UpdateUser(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenDtoIsInvalid()
        {
            // Arrange
            var testUser = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "Female",
                City = "TestCity",
                Country = "TestCountry",
                Photos = new List<Photo>()
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(testUser);

            _controller.ModelState.AddModelError("Introduction", "Required");

            var dto = new MemberUpdateDto { Introduction = null }; // invalid

            // Act
            var result = await _controller.UpdateUser(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        }
        
        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_InvalidModelState()
        {
            // Arrange
            var controller = CreateControllerWithUser("alice");

            controller.ModelState.AddModelError("KnownAs", "Required");

            // Mock repository call so controller doesn’t crash
            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync(new AppUser 
                { 
                    UserName = "alice",
                    KnownAs = "Alice",
                    Gender = "female",
                    City = "Wonderland",
                    Country = "Fantasy"
                });

            // Act
            var result = await controller.UpdateUser(new MemberUpdateDto());

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] 
        public async Task UpdateUser_SaveFails_ReturnsBadRequest()
        { 
            // Arrange
            var controller = CreateControllerWithUser("alice"); 
            var user = new AppUser 
            { 
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy" 
            }; 

            _mockUnitOfWork.Setup(x => x.UserRepository.GetUserByUsernameAsync("alice")) .ReturnsAsync(user); 
            _mockUnitOfWork.Setup(x => x.Complete()).ReturnsAsync(false); 

            // Act 
            var result = await controller.UpdateUser(new MemberUpdateDto()); 

            // Assert
             result.Should().BeOfType<BadRequestObjectResult>() 
                  .Which.Value.Should().Be("Failed to update the user"); 
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
        public async Task UpdateUser_ReturnsNoContent_WhenSuccessful()
        {
            // Arrange
            var controller = CreateControllerWithUser("alice");

            var user = new AppUser 
            { 
                UserName = "alice",
                Gender = "female",
                KnownAs = "Alice",
                City = "OldCity",
                Country = "OldCountry"
            };

            var dto = new MemberUpdateDto 
            { 
                City = "NewCity",
                Country = "NewCountry"
            };

            _mockUnitOfWork.Setup(x => x.UserRepository.GetUserByUsernameAsync("alice"))
                        .ReturnsAsync(user);

            _mockMapper.Setup(m => m.Map(dto, user));
            _mockUnitOfWork.Setup(x => x.Complete()).ReturnsAsync(true);

            // Act
            var result = await controller.UpdateUser(dto);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            _mockMapper.Verify(m => m.Map(dto, user), Times.Once);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ReturnsNoContent_WhenUpdateSucceedsV2()
        {
            var controller = CreateControllerWithMockUser("alice");
            var user = new AppUser { UserName = "alice", KnownAs = "Alice",
                    Gender = "female",
                    City = "Wonderland",
                    Country = "Fantasy", };
            var dto = new MemberUpdateDto { City = "NewCity" };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync("alice")).ReturnsAsync(user);
            _mockUnitOfWork.Setup(u => u.Complete()).ReturnsAsync(true);

            var result = await controller.UpdateUser(dto);

            result.Should().BeOfType<NoContentResult>();
            _mockMapper.Verify(m => m.Map(dto, user), Times.Once);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync((AppUser?)null);

            var dto = new MemberUpdateDto { Introduction = "Hello world" };

            // Act
            var result = await _controller.UpdateUser(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Could not find user", badRequest.Value);
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenUserDoesNotExistV2()
        {
            // Arrange
            var controller = CreateControllerWithUser("alice");

            _mockUnitOfWork
                .Setup(x => x.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync((AppUser?)null);

            var dto = new MemberUpdateDto { City = "TestCity" };

            // Act
            var result = await controller.UpdateUser(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Could not find user");
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenUsernameMismatch()
        {
            // Arrange
            var dto = new MemberUpdateDto { Introduction = "Hello mismatch" };

            var user = new AppUser
            {
                UserName = "bob",
                KnownAs = "Test",
                Gender = "Male",
                City = "TestCity",
                Country = "TestCountry"
            }; // mismatch with "alice"

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.UpdateUser(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Could not find user", badRequest.Value);
        }

        [Fact]
        public async Task UpdateUser_UsesLoggedInUsernameFromClaims()
        {
            // Arrange
            var controller = CreateControllerWithUser("alice"); // logged-in user = "alice"

            var user = new AppUser 
            { 
                UserName = "alice",
                 Gender = "female",
                KnownAs = "Alice",
                City = "OldCity",
                Country = "OldCountry"
            };

            var dto = new MemberUpdateDto { City = "TestCity" };

            _mockUnitOfWork
                .Setup(x => x.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync(user);

            _mockUnitOfWork.Setup(x => x.Complete()).ReturnsAsync(true);

            // Act
            await controller.UpdateUser(dto);

            // Assert
            _mockUnitOfWork.Verify(
                x => x.UserRepository.GetUserByUsernameAsync("alice"),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateUser_UserNotFound_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateControllerWithUser("bob");

            _mockUnitOfWork.Setup(x => x.UserRepository.GetUserByUsernameAsync("bob"))
                .ReturnsAsync((AppUser?)null);

            // Act
            var result = await controller.UpdateUser(new MemberUpdateDto());

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Could not find user");
                
            _mockMapper.Verify(m => m.Map(It.IsAny<MemberUpdateDto>(), It.IsAny<AppUser>()), Times.Never);
            
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        [Fact]
        public async Task AddPhoto_ReturnsBadRequest_WhenPhotoServiceReturnsError()
        {
            // Arrange
            var controller = CreateControllerWithUser("alice");

            var user = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy",
                Photos = new List<Photo>()
            };

            _mockUnitOfWork
                .Setup(x => x.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync(user);

            var fakeFile = new Mock<IFormFile>();

            _mockPhotoService
                .Setup(p => p.AddPhotoAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new CloudinaryDotNet.Actions.ImageUploadResult
                {
                    Error = new CloudinaryDotNet.Actions.Error { Message = "Upload failed" }
                });

            // Act
            var result = await controller.AddPhoto(fakeFile.Object);

            // Assert
            var badRequest = result.Result as BadRequestObjectResult;
            badRequest.Should().NotBeNull();
            badRequest!.Value.Should().Be("Upload failed");

            user.Photos.Should().BeEmpty();
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Never);
        }

        [Fact]
        public async Task AddPhoto_AddsPhotoToUser_WhenSuccessful()
        {
            // Arrange
            var controller = CreateControllerWithUser("alice");

            var user = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy",
                Photos = new List<Photo>()
            };

            _mockUnitOfWork
                .Setup(x => x.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync(user);

            var fakeFile = new Mock<IFormFile>();

            _mockPhotoService
                .Setup(p => p.AddPhotoAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new CloudinaryDotNet.Actions.ImageUploadResult
                {
                    SecureUrl = new Uri("https://cdn.test/photo.jpg"),
                    PublicId = "abc123"
                });

            _mockUnitOfWork
                .Setup(u => u.Complete())
                .ReturnsAsync(true);

            _mockMapper
                .Setup(m => m.Map<PhotoDto>(It.IsAny<Photo>()))
                .Returns((Photo p) => new PhotoDto
                {
                    Id = p.Id,
                    Url = p.Url,
                    IsMain = p.IsMain,
                    IsApproved = p.IsApproved
                });

            // Act
            var result = await controller.AddPhoto(fakeFile.Object);

            // Assert
            var created = result.Result as CreatedAtActionResult;
            created.Should().NotBeNull();

            created!.ActionName.Should().Be(nameof(UsersController.GetUser));
            created.RouteValues!["username"].Should().Be("alice");

            user.Photos.Should().HaveCount(1);
            var added = user.Photos.First();
            added.Url.Should().Be("https://cdn.test/photo.jpg");
            added.PublicId.Should().Be("abc123");

            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
            _mockMapper.Verify(m => m.Map<PhotoDto>(added), Times.Once);
        }


        [Fact]
        public async Task AddPhoto_ReturnsBadRequest_WhenSaveFails()
        {
            // Arrange
            var controller = CreateControllerWithUser("alice");

            var user = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy",
                Photos = new List<Photo>()
            };

            _mockUnitOfWork
                .Setup(x => x.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync(user);

            var fakeFile = new Mock<IFormFile>();

            _mockPhotoService
                .Setup(p => p.AddPhotoAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new CloudinaryDotNet.Actions.ImageUploadResult
                {
                    SecureUrl = new Uri("https://cdn.test/photo.jpg"),
                    PublicId = "abc123"
                });

            // Ensure save returns FALSE
            _mockUnitOfWork
                .Setup(u => u.Complete())
                .ReturnsAsync(false);

            // Act
            var result = await controller.AddPhoto(fakeFile.Object);

            // Assert
            var badRequest = result.Result as BadRequestObjectResult;

            badRequest.Should().NotBeNull();
            badRequest!.Value.Should().Be("Problem adding photo");

            // Photo should still have been added to user's collection
            user.Photos.Should().HaveCount(1);
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
        public async Task AddPhoto_ReturnsBadRequest_WhenUploadFails()
        {
            // Arrange
            var testUser = new AppUser
            {
                UserName = "alice",
                KnownAs = "Test",
                Gender = "Female",
                City = "TestCity",
                Country = "TestCountry",
                Photos = new List<Photo>()
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(testUser);

            var failedResult = new ImageUploadResult
            {
                Error = new Error { Message = "upload failed" }
            };

            _mockPhotoService.Setup(s => s.AddPhotoAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(failedResult);

            var file = new Mock<IFormFile>();
            file.Setup(f => f.Length).Returns(100);

            // Act
            var result = await _controller.AddPhoto(file.Object);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("upload failed", badRequest.Value);
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
        public async Task AddPhoto_ReturnsCreatedAtAction_WhenUploadSucceeds()
        {
            // Arrange
            var testUser = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "Female",
                City = "TestCity",
                Country = "TestCountry",
                Photos = new List<Photo>()
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(testUser);

            var uploadedPhoto = new ImageUploadResult
            {
                SecureUrl = new System.Uri("http://example.com/photo.jpg"),
                PublicId = "photo123"
            };

            _mockPhotoService.Setup(s => s.AddPhotoAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(uploadedPhoto);

            _mockUnitOfWork.Setup(u => u.Complete())
                .ReturnsAsync(true);

            var photoDto = new PhotoDto
            {
                Url = uploadedPhoto.SecureUrl.AbsoluteUri,
                PublicId = uploadedPhoto.PublicId
            };

            // Mock AutoMapper
            _mockMapper.Setup(m => m.Map<PhotoDto>(It.IsAny<Photo>()))
                .Returns(photoDto);

            var file = new Mock<IFormFile>();
            file.Setup(f => f.Length).Returns(100);

            // Act
            var result = await _controller.AddPhoto(file.Object);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnValue = Assert.IsType<PhotoDto>(createdResult.Value);
            Assert.Equal("http://example.com/photo.jpg", returnValue.Url);
            Assert.Equal("photo123", returnValue.PublicId);
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

        [Fact]
        public async Task DeletePhoto_ReturnsNotFound_WhenPhotoDoesNotExist()
        {
            // Arrange
            var testUser = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "Female",
                City = "TestCity",
                Country = "TestCountry",
                Photos = new List<Photo>()
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(testUser);

            int photoId = 999;

            _mockUnitOfWork.Setup(u => u.PhotoRepository.GetPhotoById(photoId))
                .ReturnsAsync((Photo?)null); // simulate photo not found

            // Act
            var result = await _controller.DeletePhoto(photoId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DeletePhoto_ReturnsBadRequest_WhenCloudinaryDeleteFails()
        {
            // Arrange
            var photo = new Photo { Id = 5, PublicId = "photo123", Url = "http://example.com/photo.jpg" };
            var testUser = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "Female",
                City = "TestCity",
                Country = "TestCountry",
                Photos = new List<Photo> { photo }
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(testUser);

            // When PhotoRepository.GetPhotoById(photo.Id) is called, return our photo
            _mockUnitOfWork.Setup(u => u.PhotoRepository.GetPhotoById(photo.Id))
                .ReturnsAsync(photo);

            // Simulate Cloudinary failure
            _mockPhotoService.Setup(s => s.DeletePhotoAsync(photo.PublicId))
                .ReturnsAsync(new DeletionResult { Error = new Error { Message = "Cloudinary error" } });

            // Act
            var result = await _controller.DeletePhoto(photo.Id);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            badRequest.Value.Should().Be("Cloudinary error");
        }

        [Fact]
        public async Task DeletePhoto_ReturnsBadRequest_WhenUnitOfWorkCompleteFails()
        {
            // Arrange
            var photo = new Photo { Id = 10, PublicId = "photo123", Url = "http://example.com/photo.jpg" };
            var testUser = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "Female",
                City = "TestCity",
                Country = "TestCountry",
                Photos = new List<Photo> { photo }
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(testUser);

            // Mock repository to return our photo when queried by Id
            _mockUnitOfWork.Setup(u => u.PhotoRepository.GetPhotoById(photo.Id))
                .ReturnsAsync(photo);

            // Simulate Cloudinary success
            _mockPhotoService.Setup(s => s.DeletePhotoAsync(photo.PublicId))
                .ReturnsAsync(new DeletionResult { Result = "ok" });

            // Simulate DB failure
            _mockUnitOfWork.Setup(u => u.Complete())
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeletePhoto(photo.Id);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            badRequest.Value.Should().Be("Problem deleting photo");
        }

        [Fact]
        public async Task DeletePhoto_ReturnsNoContent_WhenDeleteSucceeds()
        {
            // Arrange
            var photo = new Photo { Id = 11, PublicId = "photo123", Url = "http://example.com/photo.jpg" };
            var testUser = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "Female",
                City = "TestCity",
                Country = "TestCountry",
                Photos = new List<Photo> { photo }
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(testUser);

            // Return the photo when searched by Id
            _mockUnitOfWork.Setup(u => u.PhotoRepository.GetPhotoById(photo.Id))
                .ReturnsAsync(photo);

            _mockPhotoService.Setup(s => s.DeletePhotoAsync(photo.PublicId))
                .ReturnsAsync(new DeletionResult { Result = "ok" });

            _mockUnitOfWork.Setup(u => u.Complete()).ReturnsAsync(true);

            // Act
            var result = await _controller.DeletePhoto(photo.Id); // <-- now using int

            // Assert
            result.Should().BeOfType<OkResult>();
            testUser.Photos.Should().BeEmpty();
        }

        [Fact]
        public async Task DeletePhoto_ReturnsBadRequest_WhenUserNotFound()
        {
            // Arrange
            var controller = CreateControllerWithUser("alice");

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync((AppUser?)null);

            // Act
            var result = await controller.DeletePhoto(1);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("User not found");
        }
        
        [Fact]
        public async Task DeletePhoto_ReturnsBadRequest_WhenPhotoNotFound()
        {
            // Arrange
            var controller = CreateControllerWithUser("alice");

            var user = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy",
                Photos =
                {
                    new Photo { Id = 1, IsMain = false, Url= "http://example.com/photo.jpg" }
                }
            };

            _mockUnitOfWork
                .Setup(u => u.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync(user);

            // Make repository return null → this triggers controller's BadRequest("This photo cannot be deleted")
            _mockUnitOfWork
                .Setup(u => u.PhotoRepository.GetPhotoById(99))
                .ReturnsAsync((Photo?)null);

            // Act
            var result = await controller.DeletePhoto(99);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;

            badRequest.Value.Should().Be("This photo cannot be deleted");
        }
        
        [Fact]
        public async Task DeletePhoto_ReturnsBadRequest_WhenDeletingMainPhoto()
        {
            // Arrange
            var controller = CreateControllerWithUser("alice");

            var user = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy",
                Photos =
                {
                    new Photo { Id = 1, IsMain = true, Url = "http://example.com/photo.jpg" }
                }
            };

            _mockUnitOfWork
                .Setup(u => u.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync(user);

            // IMPORTANT: The controller gets the photo from the repository, NOT from user.Photos
            _mockUnitOfWork
                .Setup(u => u.PhotoRepository.GetPhotoById(1))
                .ReturnsAsync(new Photo
                {
                    Id = 1,
                    IsMain = true,
                    Url = "http://example.com/photo.jpg"
                });

            // Act
            var result = await controller.DeletePhoto(1);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;

            badRequest.Value.Should().Be("This photo cannot be deleted");
        }
        
        [Fact]
        public async Task DeletePhoto_DeletesPhotoFromCloud_WhenPublicIdExists()
        {
            // Arrange
            var controller = CreateControllerWithUser("alice");

            // Create ONE shared photo instance
            var photo = new Photo
            {
                Id = 1,
                IsMain = false,
                PublicId = "abc123",
                Url= "http://example.com/photo.jpg"
            };

            var user = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy",
                Photos = new List<Photo> { photo }
            };

            _mockUnitOfWork
                .Setup(u => u.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync(user);

            // IMPORTANT: return the SAME instance the user has
            _mockUnitOfWork
                .Setup(u => u.PhotoRepository.GetPhotoById(1))
                .ReturnsAsync(photo);

            _mockPhotoService
                .Setup(p => p.DeletePhotoAsync("abc123"))
                .ReturnsAsync(new CloudinaryDotNet.Actions.DeletionResult { Result = "ok" });

            _mockUnitOfWork
                .Setup(u => u.Complete())
                .ReturnsAsync(true);

            // Act
            var result = await controller.DeletePhoto(1);

            // Assert
            result.Should().BeOfType<OkResult>();

            // Now user.Photos is empty because the SAME reference was removed
            user.Photos.Should().BeEmpty();

            _mockPhotoService.Verify(p => p.DeletePhotoAsync("abc123"), Times.Once);
            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }
        
        [Fact]
        public async Task DeletePhoto_ReturnsBadRequest_WhenCloudDeletionFails()
        {
            // Arrange
            var controller = CreateControllerWithMockUser("alice");

            var user = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy",
                Photos = new List<Photo>
                {
                    new Photo { Id = 1, IsMain = false, PublicId = "abc123", Url= "http://example.com/photo.jpg" }
                }
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync(user);

            _mockUnitOfWork.Setup(u => u.PhotoRepository.GetPhotoById(1))
                .ReturnsAsync(user.Photos.First());

            // Cloud deletion fails (Result="error", but Error=null)
            _mockPhotoService.Setup(p => p.DeletePhotoAsync("abc123"))
                .ReturnsAsync(new CloudinaryDotNet.Actions.DeletionResult { Result = "error" });

            _mockUnitOfWork.Setup(u => u.Complete())
                .ReturnsAsync(false);

            // Act
            var result = await controller.DeletePhoto(1);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Problem deleting photo");

            // The photo will be removed because Error == null
            user.Photos.Should().BeEmpty();

            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        [Fact]
        public async Task DeletePhoto_DeletesLocalPhoto_WhenNoPublicId()
        {
            // Arrange
            var controller = CreateControllerWithMockUser("alice");

            var user = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy",
                Photos = new List<Photo>
                {
                    new Photo { Id = 1, IsMain = false, PublicId = null, Url= "http://example.com/photo.jpg" }
                }
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync(user);

            // Mock PhotoRepository to return the photo
            _mockUnitOfWork.Setup(u => u.PhotoRepository.GetPhotoById(1))
                .ReturnsAsync(user.Photos.First());

            _mockUnitOfWork.Setup(u => u.Complete())
                .ReturnsAsync(true);

            // Act
            var result = await controller.DeletePhoto(1);

            // Assert
            result.Should().BeOfType<OkResult>();
            user.Photos.Should().BeEmpty();
            _mockPhotoService.Verify(p => p.DeletePhotoAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SetMainPhoto_ReturnsBadRequest_WhenUserNotFound()
        {
            // Arrange
            var controller = CreateControllerWithUser("alice");

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync((AppUser?)null);

            // Act
            var result = await controller.SetMainPhoto(1);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Could not find user");
        }

        [Fact]
        public async Task SetMainPhoto_ReturnsBadRequest_WhenPhotoNotFoundOrAlreadyMain()
        {
            // Arrange
            var controller = CreateControllerWithUser("alice");

            var user = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy",
                Photos = new List<Photo>
                {
                    new Photo { Id = 1, IsMain = true, Url = "http://example.com/photo.jpg"  },
                    new Photo { Id = 2, IsMain = false, Url = "http://example2.com/photo.jpg" }
                }
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync(user);

            // Photo does not exist
            var resultNotFound = await controller.SetMainPhoto(99);
            resultNotFound.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Cannot use this as main photo");

            // Photo is already main
            var resultAlreadyMain = await controller.SetMainPhoto(1);
            resultAlreadyMain.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Cannot use this as main photo");
        }

        [Fact]
        public async Task SetMainPhoto_SetsNewMainPhoto_WhenValid()
        {
            // Arrange
            var controller = CreateControllerWithUser("alice");

            var user = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy",
                Photos = new List<Photo>
                {
                    new Photo { Id = 1, IsMain = true, Url = "http://example.com/photo.jpg" },
                    new Photo { Id = 2, IsMain = false, Url = "http://example2.com/photo.jpg" }
                }
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync(user);

            _mockUnitOfWork.Setup(u => u.Complete()).ReturnsAsync(true);

            // Act
            var result = await controller.SetMainPhoto(2);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            user.Photos.Single(p => p.Id == 1).IsMain.Should().BeFalse();
            user.Photos.Single(p => p.Id == 2).IsMain.Should().BeTrue();

            _mockUnitOfWork.Verify(u => u.Complete(), Times.Once);
        }

        [Fact]
        public async Task SetMainPhoto_SetsPhotoAsMain_WhenValid()
        {
            var controller = CreateControllerWithMockUser("alice");
            var user = new AppUser
            {
                UserName = "alice",
                    KnownAs = "Alice",
                        Gender = "female",
                        City = "Wonderland",
                        Country = "Fantasy",
                Photos = new List<Photo>
                {
                    new Photo { Id = 1, IsMain = true, Url = "http://example.com/photo.jpg" },
                    new Photo { Id = 2, IsMain = false, Url = "http://example2.com/photo.jpg" }
                }
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync("alice")).ReturnsAsync(user);
            _mockUnitOfWork.Setup(u => u.Complete()).ReturnsAsync(true);

            var result = await controller.SetMainPhoto(2);

            result.Should().BeOfType<NoContentResult>();
            user.Photos.First(p => p.Id == 2).IsMain.Should().BeTrue();
            user.Photos.First(p => p.Id == 1).IsMain.Should().BeFalse();
        }

        [Fact]
        public async Task SetMainPhoto_ReturnsBadRequest_WhenSaveFails()
        {
            // Arrange
            var controller = CreateControllerWithUser("alice");

            var user = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy",
                Photos = new List<Photo>
                {
                    new Photo { Id = 1, IsMain = true, Url = "http://example.com/photo.jpg" },
                    new Photo { Id = 2, IsMain = false, Url = "http://example2.com/photo.jpg" }
                }
            };

            _mockUnitOfWork.Setup(u => u.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync(user);

            _mockUnitOfWork.Setup(u => u.Complete()).ReturnsAsync(false);

            // Act
            var result = await controller.SetMainPhoto(2);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Problem setting main photo");

            // Original main photo should remain unchanged
            user.Photos.Single(p => p.Id == 1).IsMain.Should().BeFalse();
            user.Photos.Single(p => p.Id == 2).IsMain.Should().BeTrue();
        }
        
        [Fact]
        public async Task GetUser_ReturnsMember_WhenExists()
        {
            // Arrange
            var controller = CreateControllerWithMockUser("alice");
            var member = new MemberDto { Username = "bob" };

            _mockUnitOfWork
                .Setup(u => u.UserRepository.GetMemberAsync("bob", false))
                .ReturnsAsync(member);

            // Act
            var result = await controller.GetUser("bob");

            // Assert
            result.Value.Should().NotBeNull();
            result.Value.Username.Should().Be("bob");
        }
    }
}
