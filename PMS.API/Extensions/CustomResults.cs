using Microsoft.AspNetCore.Http;
using PMS.SharedKernel;

namespace PMS.API.Extensions;

public static class CustomResults
{
    public static IResult Problem(Error error)
    {
        return error.Type switch
        {
            ErrorType.NotFound => Results.NotFound(error),
            ErrorType.Conflict => Results.Conflict(error),
            ErrorType.Validation => Results.BadRequest(error),
            ErrorType.Problem => Results.BadRequest(error),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
