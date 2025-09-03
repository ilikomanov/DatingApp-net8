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

        public void Dispose()
        {
            _context.Dispose();
        }

    }
}