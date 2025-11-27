using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API.Entities;
using API.Services;
using API.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace DatingApp.Tests.Services
{
    public class TokenServiceTests
    {
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly IConfiguration _config;
        private readonly TokenService _service;

        public TokenServiceTests()
        {
            // Create mock configuration with a strong token key
            var inMemorySettings = new Dictionary<string, string>
            {
                { "TokenKey", new string('x', 64) } // valid key
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Mock UserManager
            _userManagerMock = MockUserManager();

            _service = new TokenService(_config, _userManagerMock.Object);
        }

        private static Mock<UserManager<AppUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<AppUser>>();
            return new Mock<UserManager<AppUser>>(
                store.Object, null, null, null, null, null, null, null, null
            );
        }

        private static JwtSecurityToken ReadToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ReadJwtToken(token);
        }
        
        [Fact]
        public async Task CreateToken_IncludesUserId()
        {
            var user = new AppUser
            {
                Id = 99,
                UserName = "test",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy"
            };

            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync([]);

            var token = await _service.CreateToken(user);
            var jwt = ReadToken(token);

            jwt.Claims.Should().Contain(c =>
                c.Type == "nameid" && c.Value == "99");
        }

        [Fact]
        public async Task CreateToken_IncludesRoleClaims()
        {
            var user = new AppUser
            {
                Id = 5,
                UserName = "bob",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy"
            };

            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Admin", "Moderator" });

            var token = await _service.CreateToken(user);
            var jwt = ReadToken(token);

            jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Admin");
            jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Moderator");
        }

        [Fact]
        public async Task CreateToken_ReturnsValidJwt()
        {
            var user = new AppUser
            {
                Id = 1,
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy"
            };

            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync([]);

            var token = await _service.CreateToken(user);
            var jwt = ReadToken(token);

            jwt.Should().NotBeNull();

            jwt.Claims
                .Should()
                .Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == "alice");
        }

        [Fact]
        public async Task CreateToken_Throws_WhenTokenKeyMissing()
        {
            var config = new ConfigurationBuilder().Build();

            var service = new TokenService(config, _userManagerMock.Object);

            var user = new AppUser
            {
                Id = 1,
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy"
            };

            await Assert.ThrowsAsync<Exception>(() => service.CreateToken(user));
        }

        [Fact]
        public async Task CreateToken_Throws_WhenTokenKeyTooShort()
        {
            var settings = new Dictionary<string, string>
            {
                { "TokenKey", "short" }
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var service = new TokenService(config, _userManagerMock.Object);

            var user = new AppUser
            {
                Id = 1,
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy"
            };

            await Assert.ThrowsAsync<Exception>(() => service.CreateToken(user));
        }

        [Fact]
        public async Task CreateToken_Throws_WhenUsernameIsNull()
        {
            var user = new AppUser
            {
                Id = 1,
                UserName = null!,
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy"
            };

            await Assert.ThrowsAsync<Exception>(() => _service.CreateToken(user));
        }
    }
}