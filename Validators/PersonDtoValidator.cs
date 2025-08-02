using FluentValidation;
using SqlAPI.DTOs;

namespace SqlAPI.Validators
{
    public class PersonDtoValidator : AbstractValidator<PersonDto>
    {
        public PersonDtoValidator()
        {
            RuleFor(p => p.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(p => p.Age).InclusiveBetween(0, 150).WithMessage("Age must be between 0 and 150.");
            RuleFor(p => p.Email).NotEmpty().EmailAddress().WithMessage("A valid email is required.");
        }
    }
}
