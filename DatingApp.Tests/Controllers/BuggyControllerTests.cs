using API.Controllers;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using Xunit;

namespace DatingApp.Tests.Controllers
{
    public class BuggyControllerTests : IDisposable
    {
        private readonly DataContext _context;
        private readonly BuggyController _controller;

        public BuggyControllerTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new DataContext(options);

            // Seed some users if needed
            _context.Users.Add(new AppUser {
                Id = 1,
                UserName = "alice",
                Gender = "female",
                KnownAs = "Alice",
                City = "Paris",
                Country = "France" });
            _context.SaveChanges();

            _controller = new BuggyController(_context);
        }

        [Fact]
        public void GetAuth_ReturnsSecretText()
        {
            // Act
            var result = _controller.GetAuth();

            // Assert
            var okResult = Assert.IsType<ActionResult<string>>(result);
            Assert.Equal("secret text", okResult.Value);
        }

        [Fact]
        public void GetNotFound_ReturnsNotFound()
        {
            // Act
            var result = _controller.GetNotFound();

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
