using PMS.Application.Abstractions;
using PMS.Application.Constants;
using PMS.Application.Extensions;
using PMS.Domain.Abstractions;
using PMS.Domain.Abstractions.Errors;

namespace PMS.Application.Features.UserFeatures.GetUserById;

public sealed class GetUserByIdEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/api/users/{id:guid}", async (
            Guid id,
            IHandler<GetUserByIdRequest, Result<GetUserByIdResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var request = new GetUserByIdRequest(id);
            var result = await handler.HandleAsync(request, cancellationToken);

            return result.Match(
                onSuccess: user => Results.Ok(user),
                onFailure: error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(error),
                    ErrorType.Validation => Results.BadRequest(error),
                    _ => Results.StatusCode(500)
                }
            );
        })
        .WithTags(ApiTags.Users)
        .WithName("GetUserById")
        .WithSummary("Retrieves a user by ID")
        .Produces<GetUserByIdResponse>(StatusCodes.Status200OK)
        .Produces<Error>(StatusCodes.Status404NotFound);
    }
}
