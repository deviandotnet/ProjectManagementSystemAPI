using PMS.Application.Abstractions;
using PMS.Application.Constants;
using PMS.Application.Extensions;
using PMS.Domain.Abstractions;
using PMS.Domain.Abstractions.Errors;

namespace PMS.Application.Features.UserFeatures.GetAllUsers;

public sealed class GetAllUsersEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/api/users", async (
            IHandler<GetAllUsersRequest, Result<IEnumerable<GetAllUsersResponse>>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(new GetAllUsersRequest(), cancellationToken);

            return result.Match(
                onSuccess: users => Results.Ok(users),
                onFailure: error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(error),
                    _ => Results.StatusCode(500)
                }
            );
        })
        .WithTags(ApiTags.Users)
        .WithName("GetAllUsers")
        .WithSummary("Retrieves all registered users")
        .Produces<IEnumerable<GetAllUsersResponse>>(StatusCodes.Status200OK)
        .Produces<Error>(StatusCodes.Status404NotFound);
    }
}
