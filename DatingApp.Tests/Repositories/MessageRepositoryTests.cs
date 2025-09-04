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
                Id = 1,
                UserName = "alice",
                KnownAs = "Alice",
                Gender = "female",
                City = "Wonderland",
                Country = "Fantasy",
            };

            var bob = new AppUser
            {
                Id = 2,
                UserName = "bob",
                Gender = "male",
                KnownAs = "Bobby",
                City = "TestCity",
                Country = "TestCountry",
            };

            var msg1 = new Message
            {
                Id = 1,
                Sender = alice,
                SenderUsername = "alice",
                Recipient = bob,
                RecipientUsername = "bob",
                Content = "Hi Bob",
                MessageSent = DateTime.UtcNow.AddMinutes(-10)
            };
            var msg2 = new Message
            {
                Id = 2,
                Sender = bob,
                SenderUsername = "bob",
                Recipient = alice,
                RecipientUsername = "alice",
                Content = "Hi Alice",
                MessageSent = DateTime.UtcNow.AddMinutes(-5)
            };

            var group = new Group { Name = "group1" };
            var connection = new Connection { ConnectionId = "conn1", Username = "alice" };
            group.Connections.Add(connection);

            _context.Users.AddRange(alice, bob);
            _context.Messages.AddRange(msg1, msg2);
            _context.Groups.Add(group);
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
            var group = new Group { Name = "test-group" };

            _repository.AddGroup(group);
            _context.SaveChanges();

            _context.Groups.Should().ContainSingle(g => g.Name == "test-group");
        }
    }
}