using FluentValidation;

namespace PMS.Application.Features.UserFeatures.CreateUser;

/// <summary>
/// Validator for CreateUserRequest.
/// Automatically executed by the ValidationDecorator pipeline before reaching the handler.
/// </summary>
public sealed class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .MaximumLength(100)
            .WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .MaximumLength(100)
            .WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.MiddleName)
            .MaximumLength(100)
            .WithMessage("Middle name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email address is required.")
            .EmailAddress()
            .WithMessage("A valid email address is required.")
            .MaximumLength(256)
            .WithMessage("Email address must not exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(6)
            .WithMessage("Password must be at least 6 characters long.")
            .MaximumLength(100)
            .WithMessage("Password must not exceed 100 characters.");
    }
}
