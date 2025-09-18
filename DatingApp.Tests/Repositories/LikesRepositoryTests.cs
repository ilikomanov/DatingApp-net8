using API.Data;
using API.DTOs;
using API.Entities;
using API.Helpers;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DatingApp.Tests.Repositories
{
    public class LikesRepositoryTests
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        private readonly LikesRepository _repository;

        public LikesRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new DataContext(options);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<API.Helpers.AutoMapperProfiles>();
            });
            _mapper = mapperConfig.CreateMapper();

            _repository = new LikesRepository(_context, _mapper);

            SeedData();
        }

        private void SeedData()
        {
            var alice = new AppUser
            {
                Id = 1,
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy"
            };

            var bob = new AppUser
            {
                Id = 2,
                UserName = "bob",
                KnownAs = "Bobby",
                Gender = "male",
                City = "TestCity",
                Country = "TestCountry"
            };

            var charlie = new AppUser
            {
                Id = 3,
                UserName = "charlie",
                KnownAs = "Charlie",
                Gender = "male",
                City = "TestLondon",
                Country = "TestUK"
            };

            _context.Users.AddRange(alice, bob, charlie);

            _context.Likes.AddRange(
                new UserLike { SourceUserId = 1, TargetUserId = 2 }, // Alice -> Bob
                new UserLike { SourceUserId = 2, TargetUserId = 1 }, // Bob -> Alice
                new UserLike { SourceUserId = 3, TargetUserId = 1 }  // Charlie -> Alice
            );

            _context.SaveChanges();
        }

        [Fact]
        public void AddLike_AddsLikeToContext()
        {
            var like = new UserLike { SourceUserId = 1, TargetUserId = 3 };

            _repository.AddLike(like);
            _context.SaveChanges();

            _context.Likes.Should().Contain(l => l.SourceUserId == 1 && l.TargetUserId == 3);
        }

        [Fact]
        public void DeleteLike_RemovesLikeFromContext()
        {
            var like = _context.Likes.First();

            _repository.DeleteLike(like);
            _context.SaveChanges();

            _context.Likes.Should().NotContain(l => l.SourceUserId == like.SourceUserId && l.TargetUserId == like.TargetUserId);
        }

        [Fact]
        public async Task GetCurrentUserLikeIds_ReturnsTargetIds()
        {
            var result = await _repository.GetCurrentUserLikeIds(1);

            result.Should().Contain(2);
            result.Should().NotContain(3); // Alice didn’t like Charlie
        }

        [Fact]
        public async Task GetCurrentUserLikeIds_ReturnsEmptyIfNoLikes()
        {
            var result = await _repository.GetCurrentUserLikeIds(99);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserLike_ReturnsExistingLike()
        {
            var like = await _repository.GetUserLike(1, 2);

            like.Should().NotBeNull();
            like!.SourceUserId.Should().Be(1);
            like.TargetUserId.Should().Be(2);
        }

        [Fact]
        public async Task GetUserLike_ReturnsNullIfNotFound()
        {
            var like = await _repository.GetUserLike(1, 99);

            like.Should().BeNull();
        }

        [Fact]
        public async Task GetUserLikes_ReturnsLikedUsers()
        {
            var likesParams = new LikesParams { UserId = 1, Predicate = "liked", PageNumber = 1, PageSize = 10 };

            var result = await _repository.GetUserLikes(likesParams);

            result.Should().ContainSingle();
            result.First().Username.Should().Be("bob");
        }
        
        [Fact]
        public void RemoveUserLikes_RemovesAllAssociatedLikes()
        {
            var before = _context.Likes.Count(l => l.SourceUserId == 1 || l.TargetUserId == 1);
            before.Should().BeGreaterThan(0);

            _repository.RemoveUserLikes(1);
            _context.SaveChanges();

            var after = _context.Likes.Count(l => l.SourceUserId == 1 || l.TargetUserId == 1);
            after.Should().Be(0);
        }
    }
}
