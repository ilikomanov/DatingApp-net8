using System.Security.Claims;
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
using DatingApp.Tests.TestHelpers;

namespace DatingApp.Tests.Controllers
{
    public class MessagesControllerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow = new();
        private readonly Mock<IMapper> _mockMapper = new();
        private readonly MessagesController _controller;

        public MessagesControllerTests()
        {
            _controller = new MessagesController(_mockUow.Object, _mockMapper.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "alice"),
                new Claim(ClaimTypes.NameIdentifier, "1")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task CreateMessage_ReturnsBadRequest_WhenMessagingSelf()
        {
            // Arrange
            var dto = new CreateMessageDto
            {
                RecipientUsername = "alice",
                Content = "Hello"
            };

            // Act
            var result = await _controller.CreateMessage(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result); // <-- use .Result
            Assert.Equal("You cannot message yourself", badRequest.Value);
        }
    }
}
