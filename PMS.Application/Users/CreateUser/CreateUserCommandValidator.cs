using FluentValidation;

namespace PMS.Application.Users.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(c => c.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(c => c.MiddleName)
            .MaximumLength(100)
            .When(c => !string.IsNullOrEmpty(c.MiddleName));

        RuleFor(c => c.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(c => c.Password)
            .NotEmpty()
            .MinimumLength(6)
            .MaximumLength(100);
    }
}
