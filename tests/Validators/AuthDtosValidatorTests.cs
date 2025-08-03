using FluentAssertions;
using SqlAPI.DTOs;
using SqlAPI.Validators;
using Xunit;

namespace SqlAPI.Tests.Validators
{
    public class AuthDtosValidatorTests
    {
        [Fact]
        public void RegisterDtoValidator_ValidData_ShouldPass()
        {
            // Arrange
            var validator = new RegisterDtoValidator();
            var registerDto = new RegisterDto
            {
                Username = "testuser",
                Password = "Password123",
                ConfirmPassword = "Password123",
                Role = "User"
            };

            // Act
            var result = validator.Validate(registerDto);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void RegisterDtoValidator_InvalidUsername_ShouldFail()
        {
            // Arrange
            var validator = new RegisterDtoValidator();
            var registerDto = new RegisterDto
            {
                Username = "ab", // Too short
                Password = "Password123",
                ConfirmPassword = "Password123",
                Role = "User"
            };

            // Act
            var result = validator.Validate(registerDto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Username");
        }

        [Fact]
        public void RegisterDtoValidator_WeakPassword_ShouldFail()
        {
            // Arrange
            var validator = new RegisterDtoValidator();
            var registerDto = new RegisterDto
            {
                Username = "testuser",
                Password = "weak", // No uppercase, no digits
                ConfirmPassword = "weak",
                Role = "User"
            };

            // Act
            var result = validator.Validate(registerDto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Password");
        }

        [Fact]
        public void RegisterDtoValidator_PasswordMismatch_ShouldFail()
        {
            // Arrange
            var validator = new RegisterDtoValidator();
            var registerDto = new RegisterDto
            {
                Username = "testuser",
                Password = "Password123",
                ConfirmPassword = "DifferentPassword123",
                Role = "User"
            };

            // Act
            var result = validator.Validate(registerDto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "ConfirmPassword");
        }

        [Fact]
        public void RegisterDtoValidator_InvalidRole_ShouldFail()
        {
            // Arrange
            var validator = new RegisterDtoValidator();
            var registerDto = new RegisterDto
            {
                Username = "testuser",
                Password = "Password123",
                ConfirmPassword = "Password123",
                Role = "InvalidRole"
            };

            // Act
            var result = validator.Validate(registerDto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Role");
        }

        [Fact]
        public void LoginDtoValidator_ValidData_ShouldPass()
        {
            // Arrange
            var validator = new LoginDtoValidator();
            var loginDto = new LoginDto
            {
                Username = "testuser",
                Password = "Password123"
            };

            // Act
            var result = validator.Validate(loginDto);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void LoginDtoValidator_EmptyUsername_ShouldFail()
        {
            // Arrange
            var validator = new LoginDtoValidator();
            var loginDto = new LoginDto
            {
                Username = "",
                Password = "Password123"
            };

            // Act
            var result = validator.Validate(loginDto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Username");
        }

        [Fact]
        public void LoginDtoValidator_EmptyPassword_ShouldFail()
        {
            // Arrange
            var validator = new LoginDtoValidator();
            var loginDto = new LoginDto
            {
                Username = "testuser",
                Password = ""
            };

            // Act
            var result = validator.Validate(loginDto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Password");
        }
    }
}