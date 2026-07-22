using PMS.Application.Abstractions;
using PMS.Application.Constants;
using PMS.Application.Extensions;
using PMS.Domain.Abstractions;
using PMS.Domain.Abstractions.Errors;

namespace PMS.Application.Features.UserFeatures.CreateUser;

/// <summary>
/// Minimal API endpoint: POST /api/users
/// Creates a new user account in the system.
/// </summary>
public sealed class CreateUserEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/api/users", async (
            CreateUserRequest request,
            IHandler<CreateUserRequest, Result<CreateUserResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(request, cancellationToken);

            return result.Match(
                onSuccess: user => Results.Created($"/api/users/{user.Id}", user),
                onFailure: error => error.Type switch
                {
                    ErrorType.Conflict => Results.Conflict(error),
                    ErrorType.Validation => Results.BadRequest(error),
                    _ => Results.StatusCode(500)
                }
            );
        })
        .WithTags(ApiTags.Users)
        .WithName("CreateUser")
        .WithSummary("Creates a new user account")
        .WithDescription("Creates a new user entity and returns the created user details (excluding password hash). Returns 409 if email already exists.")
        .Produces<CreateUserResponse>(StatusCodes.Status201Created)
        .Produces<Error>(StatusCodes.Status400BadRequest)
        .Produces<Error>(StatusCodes.Status409Conflict);
    }
}
