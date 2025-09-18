using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DatingApp.Tests.Repositories
{
    public class PhotoRepositoryTests
    {
        private readonly DataContext _context;
        private readonly PhotoRepository _repository;

        public PhotoRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new DataContext(options);
            _repository = new PhotoRepository(_context);

            SeedData();
        }

        private void SeedData()
        {
            var user = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy"
            };

            _context.Users.Add(user);

            _context.Photos.AddRange(
                new Photo { Url = "http://photo1.com", AppUser = user, IsApproved = false, AppUserId = 1 },
                new Photo { Url = "http://photo2.com", AppUser = user, IsApproved = true, AppUserId = 2 }
            );

            _context.SaveChanges();
        }

        [Fact]
        public async Task GetPhotoById_ReturnsPhoto_WhenExists()
        {
            // Arrange
            var expectedPhoto = _context.Photos.First(); // because we know we seeded it

            // Act
            var photo = await _repository.GetPhotoById(expectedPhoto.Id);

            // Assert
            photo.Should().NotBeNull();
            photo!.Url.Should().Be(expectedPhoto.Url);
        }

        [Fact]
        public async Task GetPhotoById_ReturnsNull_WhenNotFound()
        {
            var photo = await _repository.GetPhotoById(999);
            photo.Should().BeNull();
        }

        [Fact]
        public async Task GetUnapprovedPhotos_ReturnsOnlyUnapproved()
        {
            var photos = await _repository.GetUnapprovedPhotos();

            photos.Should().ContainSingle(p => p.IsApproved == false);
        }

        [Fact]
        public void RemovePhoto_RemovesSinglePhoto()
        {
            var photo = _context.Photos.First();
            _repository.RemovePhoto(photo);
            _context.SaveChanges();

            _context.Photos.Should().NotContain(photo);
        }
    }
}