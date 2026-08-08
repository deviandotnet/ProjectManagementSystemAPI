using Microsoft.AspNetCore.Http;
using PMS.SharedKernel;

namespace PMS.API.Extensions;

public static class CustomResults
{
    public static IResult Problem(Error error)
    {
        if (error.Code.EndsWith(".Unauthorized") || error.Code == "Users.Unauthorized")
        {
            return Results.Json(error, statusCode: StatusCodes.Status401Unauthorized);
        }

        if (error.Code.EndsWith(".Forbidden") ||
            error.Code.EndsWith(".NotProjectMember") ||
            error.Code.EndsWith(".ReadOnlyAccess") ||
            error.Code.EndsWith(".AdminOnly"))
        {
            return Results.Json(error, statusCode: StatusCodes.Status403Forbidden);
        }

        return error.Type switch
        {
            ErrorType.NotFound => Results.NotFound(error),
            ErrorType.Conflict => Results.Conflict(error),
            ErrorType.Validation => Results.BadRequest(error),
            ErrorType.Problem => Results.BadRequest(error),
            ErrorType.Failure => Results.BadRequest(error),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
