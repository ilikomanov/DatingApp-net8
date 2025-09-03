using API.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DatingApp.Tests.DTOs
{
    public class RegisterDtoTests
    {
        private IList<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }

        private RegisterDto CreateValidDto() => new RegisterDto
        {
            Username = "alice",
            KnownAs = "Alice",
            Gender = "Female",
            DateOfBirth = "1990-01-01",
            City = "Wonderland",
            Country = "FantasyLand",
            Password = "secret"
        };

        [Fact]
        public void RegisterDto_Invalid_WhenUsernameMissing()
        {
            var dto = CreateValidDto();
            dto.Username = null!;

            var results = ValidateModel(dto);

            results.Should().ContainSingle()
                .Which.MemberNames.Should().Contain("Username");
        }
    }
}