using FluentValidation;

namespace PMS.Application.Users.LoginUser;

public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(c => c.Password)
            .NotEmpty()
            .MinimumLength(6);
    }
}
