using PMS.Application.Abstractions;
using Microsoft.Extensions.Logging;
using PMS.Domain.Abstractions;

namespace PMS.Application.Pipelines;

public sealed class LoggingDecorator<TRequest, TResponse>(
    ILogger<LoggingDecorator<TRequest, TResponse>> logger,
    IHandler<TRequest, TResponse> innerHandler) : IHandler<TRequest, TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest command, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Handling request {RequestName}", requestName);

        var response = await innerHandler.HandleAsync(command, cancellationToken);

        if (response is Result result && result.IsFailure)
        {
            logger.LogWarning(
                "Request {RequestName} failed with error code {ErrorCode}",
                requestName,
                result.Error.Code);
        }
        else
        {
            logger.LogInformation("Handled request {RequestName}", requestName);
        }

        return response;
    }
}
