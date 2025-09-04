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
    public class MessageRepositoryTests
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        private readonly MessageRepository _repository;

        public MessageRepositoryTests()
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

            _repository = new MessageRepository(_context, _mapper);

            SeedData();
        }

        private void SeedData()
        {
            var alice = new AppUser
            {
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy",
            };
            var bob = new AppUser
            {
                UserName = "bob",
                Gender = "male",
                KnownAs = "Bobby",
                City = "TestCity",
                Country = "TestCountry",
            };
            _context.Users.AddRange(alice, bob);

            _context.Messages.AddRange(
                new Message
                {
                    Sender = alice,
                    SenderUsername = "alice",
                    Recipient = bob,
                    RecipientUsername = "bob",
                    Content = "Hi Bob",
                    MessageSent = DateTime.UtcNow
                },
                new Message
                {
                    Sender = bob,
                    SenderUsername = "bob",
                    Recipient = alice,
                    RecipientUsername = "alice",
                    Content = "Hi Alice",
                    MessageSent = DateTime.UtcNow
                }
            );

            // Important: set Username for Connection
            var connection = new Connection
            {
                ConnectionId = "123",
                Username = "alice"
            };

            var group = new Group
            {
                Name = "test-group",
                Connections = new List<Connection> { connection }
            };

            _context.Groups.Add(group);
            _context.Connections.Add(connection);

            _context.SaveChanges();
        }

        [Fact]
        public void AddMessage_AddsMessageToContext()
        {
            var message = new Message
            {
                SenderUsername = "alice",
                RecipientUsername = "bob",
                Content = "new message"
            };

            _repository.AddMessage(message);
            _context.SaveChanges();

            _context.Messages.Should().ContainSingle(m =>
                m.SenderUsername == "alice" &&
                m.RecipientUsername == "bob" &&
                m.Content == "new message");
        }

        [Fact]
        public void DeleteMessage_RemovesMessageFromContext()
        {
            var message = new Message
            {
                SenderUsername = "alice",
                RecipientUsername = "bob",
                Content = "temp"
            };
            _context.Messages.Add(message);
            _context.SaveChanges();

            _repository.DeleteMessage(message);
            _context.SaveChanges();

            _context.Messages.Should().NotContain(m =>
                m.SenderUsername == "alice" &&
                m.RecipientUsername == "bob" &&
                m.Content == "temp");
        }

        [Fact]
        public void AddGroup_AddsGroupToContext()
        {
            var group = new Group { Name = "new-group" };

            _repository.AddGroup(group);
            _context.SaveChanges();

            _context.Groups.Should().Contain(g => g.Name == "new-group");
        }

        [Fact]
        public async Task GetMessage_ReturnsMessage()
        {
            var message = await _repository.GetMessage(_context.Messages.First().Id);
            message.Should().NotBeNull();
        }

        [Fact]
        public async Task GetMessagesForUser_ReturnsInboxMessages()
        {
            // Arrange
            var messageParams = new MessageParams
            {
                Username = "alice",
                Container = "Inbox",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _repository.GetMessagesForUser(messageParams);

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(m => m.RecipientUsername == "alice");
        }

        [Fact]
        public async Task GetMessagesForUser_ReturnsOutboxMessages()
        {
            // Arrange
            var messageParams = new MessageParams
            {
                Username = "alice",
                Container = "Outbox",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _repository.GetMessagesForUser(messageParams);

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(m => m.SenderUsername == "alice");
        }

        
        [Fact]
        public async Task GetMessagesForUser_ReturnsEmpty_WhenNoMessagesExist()
        {
            var messageParams = new MessageParams
            {
                Username = "nonexistent",
                Container = "Inbox",
                PageNumber = 1,
                PageSize = 10
            };

            var result = await _repository.GetMessagesForUser(messageParams);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMessageGroup_ReturnsGroupByName()
        {
            // Arrange
            var groupName = "test-group";

            // Act
            var group = await _repository.GetMessageGroup(groupName);

            // Assert
            group.Should().NotBeNull();
            group!.Name.Should().Be(groupName);
            group.Connections.Should().ContainSingle(c => c.ConnectionId == "123");
        }

        [Fact]
        public async Task GetMessageThread_ReturnsMessagesInOrder()
        {
            var messages = await _repository.GetMessageThread("alice", "bob");
            messages.Should().HaveCount(2);
            messages.First().SenderUsername.Should().Be("alice");
        }

        [Fact]
        public async Task GetConnection_ReturnsConnection()
        {
            var connection = await _repository.GetConnection("123");
            connection.Should().NotBeNull();
            connection!.Username.Should().Be("alice");
        }

        [Fact]
        public async Task GetGroupForConnection_ReturnsGroupContainingConnection()
        {
            // Arrange
            var connectionId = "123";

            // Act
            var group = await _repository.GetGroupForConnection(connectionId);

            // Assert
            group.Should().NotBeNull();
            group!.Connections.Should().ContainSingle(c => c.ConnectionId == connectionId);
        }

        [Fact]
        public void RemoveUserMessages_DeletesAllUserMessages()
        {
            // Arrange
            var aliceMessagesBefore = _context.Messages.Count(m => m.SenderUsername == "alice" || m.RecipientUsername == "alice");
            aliceMessagesBefore.Should().BeGreaterThan(0);

            // Act
            _repository.RemoveUserMessages("alice");
            _context.SaveChanges();

            // Assert
            var aliceMessagesAfter = _context.Messages.Count(m => m.SenderUsername == "alice" || m.RecipientUsername == "alice");
            aliceMessagesAfter.Should().Be(0);
        }

        [Fact]
        public void RemoveConnection_RemovesConnectionFromContext()
        {
            var connection = _context.Connections.First();
            _repository.RemoveConnection(connection);
            _context.SaveChanges();

            _context.Connections.Should().NotContain(c => c.ConnectionId == connection.ConnectionId);
        }
    }
}