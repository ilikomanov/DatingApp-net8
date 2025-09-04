using API.Data;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatingApp.Tests.Repositories
{
    public class UserRepositoryTests : IDisposable
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        private readonly UserRepository _repository;

        public UserRepositoryTests()
        {
            // Setup InMemory database
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique db for each test
                .Options;
            _context = new DataContext(options);

            // Setup AutoMapper
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperProfiles()); // ensure AutoMapperProfiles exists in API.Helpers
            });
            
            _mapper = config.CreateMapper();

            _repository = new UserRepository(_context, _mapper);

            SeedData();
        }

        private void SeedData()
        {
            var users = new List<AppUser>
            {
                new AppUser
                {
                    Id = 1,
                    UserName = "alice",
                    KnownAs = "Alice",
                    Gender = "female",
                    City = "Wonderland",
                    Country = "Fantasy",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-25)),
                    Created = DateTime.UtcNow.AddDays(-10),
                    LastActive = DateTime.UtcNow.AddDays(-1),
                    Photos = new List<Photo>
                    {
                        new Photo { Id = 101, Url = "http://example.com/alice1.jpg", IsMain = true }
                    }
                },

                new AppUser
                {
                    Id = 2,
                    UserName = "bob",
                    Gender = "male",
                    KnownAs = "Bobby",
                    City = "TestCity",
                    Country = "TestCountry",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-30)),
                    Created = DateTime.UtcNow.AddDays(-20),
                    LastActive = DateTime.UtcNow.AddDays(-5),
                    Photos = new List<Photo>
                    {
                        new Photo { Id = 102, Url = "http://example.com/bob1.jpg", IsMain = true }
                    }
                }
            };

            _context.Users.AddRange(users);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetUserByIdAsync_ReturnsCorrectUser()
        {
            var user = await _repository.GetUserByIdAsync(1);
            user.Should().NotBeNull();
            user!.UserName.Should().Be("alice");
        }

        [Fact]
        public async Task GetUserByIdAsync_ReturnsNull_WhenNotFound()
        {
            var user = await _repository.GetUserByIdAsync(999);
            user.Should().BeNull();
        }

        [Fact]
        public async Task GetUserByUsernameAsync_ReturnsUserWithPhotos()
        {
            var user = await _repository.GetUserByUsernameAsync("bob");
            user.Should().NotBeNull();
            user!.Photos.Should().ContainSingle(p => p.Url.Contains("bob1"));
        }

        [Fact]
        public async Task GetUserByUsernameAsync_ReturnsNull_WhenNotFound()
        {
            var user = await _repository.GetUserByUsernameAsync("nonexistent");
            user.Should().BeNull();
        }

        [Fact]
        public async Task GetUserByPhotoId_ReturnsCorrectUser()
        {
            var user = await _repository.GetUserByPhotoId(101);
            user.Should().NotBeNull();
            user!.UserName.Should().Be("alice");
        }

        [Fact]
        public async Task GetUserByPhotoId_ReturnsNull_WhenNoUserHasPhoto()
        {
            var user = await _repository.GetUserByPhotoId(999);
            user.Should().BeNull();
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsAllUsers()
        {
            var users = await _repository.GetUsersAsync();
            users.Should().HaveCount(2);
        }

        [Fact]
        public void Update_SetsEntityStateToModified()
        {
            var user = _context.Users.First();
            _repository.Update(user);
            _context.Entry(user).State.Should().Be(EntityState.Modified);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}