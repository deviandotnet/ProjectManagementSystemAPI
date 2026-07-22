using FluentValidation;

namespace PMS.Application.Features.ProjectFeatures.GetProjectById;

/// <summary>
/// Validator for GetProjectByIdRequest.
/// Runs automatically via the ValidationDecorator pipeline before the handler executes.
/// Ensures the ProjectId is not an empty GUID.
/// </summary>
public sealed class GetProjectByIdValidator : AbstractValidator<GetProjectByIdRequest>
{
    public GetProjectByIdValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("Project ID must not be empty.");
    }
}
