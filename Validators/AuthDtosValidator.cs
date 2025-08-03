using FluentValidation;
using SqlAPI.DTOs;

namespace SqlAPI.Validators
{
    /// <summary>
    /// Validator for RegisterDto
    /// </summary>
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.")
                .Length(3, 50).WithMessage("Username must be between 3 and 50 characters.")
                .Matches("^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers, and underscores.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)").WithMessage("Password must contain at least one lowercase letter, one uppercase letter, and one digit.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Password confirmation is required.")
                .Equal(x => x.Password).WithMessage("Password and confirmation password do not match.");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role is required.")
                .Must(role => role == "User" || role == "Admin").WithMessage("Role must be either 'User' or 'Admin'.");
        }
    }

    /// <summary>
    /// Validator for LoginDto
    /// </summary>
    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.")
                .Length(3, 50).WithMessage("Username must be between 3 and 50 characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
        }
    }
}