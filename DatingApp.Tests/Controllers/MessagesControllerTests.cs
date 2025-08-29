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
        private readonly Mock<IMessageRepository> _mockMessageRepo = new();
        private readonly Mock<IUserRepository> _mockUserRepo = new();

        public MessagesControllerTests()
        {
            _mockUow.Setup(u => u.UserRepository).Returns(_mockUserRepo.Object);
            _mockUow.Setup(u => u.MessageRepository).Returns(_mockMessageRepo.Object);
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
        public async Task CreateMessage_ReturnsOk_WhenMessageSaved()
        {
            // Arrange
            var dto = new CreateMessageDto
            {
                RecipientUsername = "bob",
                Content = "Hello"
            };

            var sender = new AppUser
            {
                UserName = "alice",
                Gender = "female",
                KnownAs = "Alice",
                City = "Paris",
                Country = "France"
            };

            var recipient = new AppUser
            {
                UserName = "bob",
                Gender = "male",
                KnownAs = "Bob",
                City = "London",
                Country = "UK"
            };

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = "alice",
                RecipientUsername = "bob",
                Content = "Hello"
            };

            var messageDto = new MessageDto
            {
                Id = 1,
                Content = "Hello",
                SenderUsername = "alice",
                RecipientUsername = "bob",
                SenderPhotoUrl = "sender-photo.jpg",
                RecipientPhotoUrl = "recipient-photo.jpg"
            };

            _mockUow.Setup(u => u.UserRepository.GetUserByUsernameAsync("alice")).ReturnsAsync(sender);
            _mockUow.Setup(u => u.UserRepository.GetUserByUsernameAsync("bob")).ReturnsAsync(recipient);
            _mockMapper.Setup(m => m.Map<MessageDto>(It.IsAny<Message>())).Returns(messageDto);
            _mockUow.Setup(u => u.Complete()).ReturnsAsync(true);

            // Act
            var result = await _controller.CreateMessage(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedDto = Assert.IsType<MessageDto>(okResult.Value);
            Assert.Equal("alice", returnedDto.SenderUsername);
            Assert.Equal("bob", returnedDto.RecipientUsername);
            Assert.Equal("Hello", returnedDto.Content);
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

        [Fact]
        public async Task CreateMessage_ReturnsBadRequest_WhenRecipientNotFound()
        {
            // Arrange
            var dto = new CreateMessageDto
            {
                RecipientUsername = "bob",
                Content = "Hello"
            };

            _mockUow.Setup(u => u.UserRepository.GetUserByUsernameAsync("alice"))
                .ReturnsAsync(new AppUser
                {
                    UserName = "alice",
                    Gender = "female",
                    KnownAs = "Alice",
                    City = "Paris",
                    Country = "France"
                });
            _mockUow.Setup(u => u.UserRepository.GetUserByUsernameAsync("bob"))
                .ReturnsAsync((AppUser?)null); // recipient not found

            // Act
            var result = await _controller.CreateMessage(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Cannot send message at this time", badRequest.Value);
        }

        [Fact]
        public async Task GetMessagesForUser_ReturnsPagedMessages()
        {
            // Arrange
            var username = "alice";
            var messageParams = new MessageParams(); // defaults
            var messages = new List<MessageDto>
            {
                new MessageDto { Id = 1, Content = "Hello", SenderUsername = "alice", RecipientUsername = "bob", SenderPhotoUrl="s.jpg", RecipientPhotoUrl="r.jpg" },
                new MessageDto { Id = 2, Content = "Hi", SenderUsername = "bob", RecipientUsername = "alice", SenderPhotoUrl="r.jpg", RecipientPhotoUrl="s.jpg" }
            };

            var pagedList = new PagedList<MessageDto>(messages, messages.Count, 1, messages.Count);

            _mockUow.Setup(u => u.MessageRepository.GetMessagesForUser(It.Is<MessageParams>(m => m.Username == username)))
                .ReturnsAsync(pagedList);

            // Act
            var result = await _controller.GetMessagesForUser(messageParams);

            // Assert
            // Access Value directly instead of Result
            var returnedMessages = Assert.IsAssignableFrom<PagedList<MessageDto>>(result.Value);
            Assert.Equal(2, returnedMessages.Count);

            // You can still verify the Pagination header
            Assert.True(_controller.Response.Headers.ContainsKey("Pagination"));
        }

        [Fact]
        public async Task GetMessageThread_ReturnsOk_WithMessages()
        {
            // Arrange
            var currentUsername = "alice";
            var otherUsername = "bob";

            var messages = new List<MessageDto>
            {
                new MessageDto
                {
                    Id = 1,
                    Content = "Hello Bob",
                    SenderUsername = "alice",
                    RecipientUsername = "bob",
                    SenderPhotoUrl = "alice.jpg",
                    RecipientPhotoUrl = "bob.jpg"
                },
                new MessageDto
                {
                    Id = 2,
                    Content = "Hi Alice",
                    SenderUsername = "bob",
                    RecipientUsername = "alice",
                    SenderPhotoUrl = "bob.jpg",
                    RecipientPhotoUrl = "alice.jpg"
                }
            };

            _mockMessageRepo
                .Setup(r => r.GetMessageThread(currentUsername, otherUsername))
                .ReturnsAsync(messages);

            // Act
            var result = await _controller.GetMessageThread(otherUsername);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedMessages = Assert.IsAssignableFrom<IEnumerable<MessageDto>>(okResult.Value);

            Assert.Equal(2, returnedMessages.Count());
            Assert.Contains(returnedMessages, m => m.Content == "Hello Bob");
            Assert.Contains(returnedMessages, m => m.Content == "Hi Alice");
        }

    }
}
