using FluentValidation;

namespace PMS.Application.Features.UserFeatures.GetUserById;

/// <summary>
/// Validator for GetUserByIdRequest.
/// Ensures that the provided UserId is not an empty GUID.
/// </summary>
public sealed class GetUserByIdValidator : AbstractValidator<GetUserByIdRequest>
{
    public GetUserByIdValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID must not be empty.");
    }
}
