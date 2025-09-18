using API.Helpers;
using API.Services;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DatingApp.Tests.Services
{
    public class PhotoServiceTests
    {
        private readonly PhotoService _service;

        public PhotoServiceTests()
        {
            var cloudinarySettings = Options.Create(new CloudinarySettings
            {
                CloudName = "test",
                ApiKey = "key",
                ApiSecret = "secret"
            });

            _service = new PhotoService(cloudinarySettings);
        }

        [Fact]
        public async Task AddPhotoAsync_ReturnsResult_WhenFileIsValid()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1);
            fileMock.Setup(f => f.FileName).Returns("test.jpg");
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[10]));

            var result = await _service.AddPhotoAsync(fileMock.Object);

            result.Should().NotBeNull();
            // Note: Not actually hitting Cloudinary, so this is more of an integration test if left as-is.
        }

        [Fact]
        public async Task AddPhotoAsync_ReturnsEmptyResult_WhenFileIsEmpty()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(0);

            var result = await _service.AddPhotoAsync(fileMock.Object);

            result.Should().NotBeNull();
            result.PublicId.Should().BeNull();
        }

        [Fact]
        public async Task DeletePhotoAsync_ReturnsResult()
        {
            var result = await _service.DeletePhotoAsync("test-id");

            result.Should().NotBeNull();
        }
    }
}