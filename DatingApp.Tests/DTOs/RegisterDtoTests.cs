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

        [Fact]
        public void RegisterDto_Invalid_WhenKnownAsMissing()
        {
            var dto = CreateValidDto();
            dto.KnownAs = null!;

            var results = ValidateModel(dto);

            results.Should().ContainSingle()
                .Which.MemberNames.Should().Contain("KnownAs");
        }

        [Fact]
        public void RegisterDto_Invalid_WhenGenderMissing()
        {
            var dto = CreateValidDto();
            dto.Gender = null!;

            var results = ValidateModel(dto);

            results.Should().ContainSingle()
                .Which.MemberNames.Should().Contain("Gender");
        }

        [Fact]
        public void RegisterDto_Invalid_WhenDateOfBirth()
        {
            var dto = CreateValidDto();
            dto.DateOfBirth = null!;

            var results = ValidateModel(dto);

            results.Should().ContainSingle()
                .Which.MemberNames.Should().Contain("DateOfBirth");
        }

        [Fact]
        public void RegisterDto_Invalid_WhenCityMissing()
        {
            var dto = CreateValidDto();
            dto.City = null!;

            var results = ValidateModel(dto);

            results.Should().ContainSingle()
                .Which.MemberNames.Should().Contain("City");
        }

        [Fact]
        public void RegisterDto_Invalid_WhenCountryMissing()
        {
            var dto = CreateValidDto();
            dto.Country = null!;

            var results = ValidateModel(dto);

            results.Should().ContainSingle()
                .Which.MemberNames.Should().Contain("Country");
        }

        [Fact]
        public void RegisterDto_Invalid_WhenPasswordTooShort()
        {
            var dto = CreateValidDto();
            dto.Password = "abc"; // 3 chars, invalid

            var results = ValidateModel(dto);

            results.Should().ContainSingle()
                .Which.ErrorMessage.Should().Contain("minimum length of 4 and a maximum length of 8");
        }

        [Fact]
        public void RegisterDto_Invalid_WhenPasswordTooLong()
        {
            var dto = CreateValidDto();
            dto.Password = "toolongpw"; // 9 chars, invalid

            var results = ValidateModel(dto);

            results.Should().ContainSingle()
                .Which.ErrorMessage.Should().Contain("minimum length of 4 and a maximum length of 8");
        }
    }
}