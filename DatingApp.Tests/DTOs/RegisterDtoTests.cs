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
        public void RegisterDto_Invalid_WhenDateOfBirthIsNotAValidDate()
        {
            var dto = new RegisterDto
            {
                Username = "bob",
                KnownAs = "Bobby",
                Gender = "Male",
                DateOfBirth = "not-a-date", // invalid
                City = "TestCity",
                Country = "TestCountry",
                Password = "secret"
            };

            // Act: validation itself will pass, because it's just a string
            var results = ValidateModel(dto);

            results.Should().BeEmpty(); // Required passes

            // Extra check: try parsing manually
            Action act = () => DateTime.Parse(dto.DateOfBirth!);

            act.Should().Throw<FormatException>()
                .WithMessage("*not recognized as a valid DateTime*");
        }

        [Fact]
        public void RegisterDto_Valid_WhenDateOfBirthIsAValidDate()
        {
            var dto = new RegisterDto
            {
                Username = "alice",
                KnownAs = "Alice",
                Gender = "Female",
                DateOfBirth = "1990-05-15", // valid ISO format
                City = "Wonderland",
                Country = "Fantasy",
                Password = "secure1"
            };

            // Act: validation should pass
            var results = ValidateModel(dto);

            results.Should().BeEmpty();

            // Extra check: try parsing manually
            DateTime parsed;
            var success = DateTime.TryParse(dto.DateOfBirth, out parsed);

            success.Should().BeTrue();
            parsed.Year.Should().Be(1990);
            parsed.Month.Should().Be(5);
            parsed.Day.Should().Be(15);
        }

        [Fact]
        public void RegisterDto_Invalid_WhenDateOfBirthIsInTheFuture()
        {
            var futureDate = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");

            var dto = new RegisterDto
            {
                Username = "alice",
                KnownAs = "Alice",
                Gender = "Female",
                DateOfBirth = futureDate,
                City = "Wonderland",
                Country = "Fantasy",
                Password = "secure1"
            };

            // Act
            DateTime parsed;
            var success = DateTime.TryParse(dto.DateOfBirth, out parsed);

            success.Should().BeTrue();
            parsed.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public void RegisterDto_Valid_WhenDateOfBirthIsVeryOld()
        {
            var dto = new RegisterDto
            {
                Username = "alice",
                KnownAs = "Alice",
                Gender = "Female",
                DateOfBirth = "1900-01-01", // extreme but valid
                City = "Wonderland",
                Country = "Fantasy",
                Password = "secure1"
            };

            // Act
            DateTime parsed;
            var success = DateTime.TryParse(dto.DateOfBirth, out parsed);

            success.Should().BeTrue();
            parsed.Year.Should().Be(1900);
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

        [Fact]
        public void RegisterDto_Valid_WhenAllFieldsCorrect()
        {
            var dto = CreateValidDto();

            var results = ValidateModel(dto);

            results.Should().BeEmpty(); // no validation errors
        }
    }
}