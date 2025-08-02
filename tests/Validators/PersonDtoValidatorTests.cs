using Xunit;
using FluentAssertions;
using SqlAPI.Validators;
using SqlAPI.DTOs;

namespace SqlAPI.Tests.Validators
{
    public class PersonDtoValidatorTests
    {
        private readonly PersonDtoValidator _validator = new PersonDtoValidator();

        [Theory]
        [InlineData(null, 30, "test@test.com")]
        [InlineData("", 30, "test@test.com")]
        [InlineData("  ", 30, "test@test.com")]
        public void Should_Have_Error_When_Name_Is_Null_Or_Empty(string name, int age, string email)
        {
            // Arrange
            var personDto = new PersonDto { Name = name, Age = age, Email = email };

            // Act
            var result = _validator.Validate(personDto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }

        [Theory]
        [InlineData("Test Name", -1, "test@test.com")]
        [InlineData("Test Name", 151, "test@test.com")]
        public void Should_Have_Error_When_Age_Is_Outside_Valid_Range(string name, int age, string email)
        {
            // Arrange
            var personDto = new PersonDto { Name = name, Age = age, Email = email };

            // Act
            var result = _validator.Validate(personDto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Age");
        }

        [Theory]
        [InlineData("Test Name", 30, "not-an-email")]
        [InlineData("Test Name", 30, "")]
        [InlineData("Test Name", 30, null)]
        public void Should_Have_Error_When_Email_Is_Invalid(string name, int age, string email)
        {
            // Arrange
            var personDto = new PersonDto { Name = name, Age = age, Email = email };

            // Act
            var result = _validator.Validate(personDto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Email");
        }

        [Fact]
        public void Should_Not_Have_Error_When_Dto_Is_Valid()
        {
            // Arrange
            var personDto = new PersonDto { Name = "John Doe", Age = 42, Email = "john.doe@example.com" };

            // Act
            var result = _validator.Validate(personDto);

            // Assert
            result.IsValid.Should().BeTrue();
        }
    }
}
