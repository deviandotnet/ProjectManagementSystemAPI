using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PMS.Application.Abstractions.Messaging;
using PMS.SharedKernel;
using Serilog.Context;

namespace PMS.Application.Abstractions.Behaviors;

internal static class LoggingDecorator
{
    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        ILogger<CommandHandler<TCommand, TResponse>> logger)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            string commandName = typeof(TCommand).Name;

            logger.LogInformation("Processing command {Command}", commandName);

            var stopwatch = Stopwatch.StartNew();
            Result<TResponse> result = await innerHandler.Handle(command, cancellationToken);
            stopwatch.Stop();

            if (result.IsSuccess)
            {
                logger.LogInformation(
                    "Completed command {Command} in {ElapsedMilliseconds} ms",
                    commandName,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                using (LogContext.PushProperty("ErrorCode", result.Error.Code))
                {
                    logger.LogError(
                        "Completed command {Command} with error ({ErrorCode}) in {ElapsedMilliseconds} ms",
                        commandName,
                        result.Error.Code,
                        stopwatch.ElapsedMilliseconds);
                }
            }

            return result;
        }
    }

    internal sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        ILogger<CommandBaseHandler<TCommand>> logger)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            string commandName = typeof(TCommand).Name;

            logger.LogInformation("Processing command {Command}", commandName);

            var stopwatch = Stopwatch.StartNew();
            Result result = await innerHandler.Handle(command, cancellationToken);
            stopwatch.Stop();

            if (result.IsSuccess)
            {
                logger.LogInformation(
                    "Completed command {Command} in {ElapsedMilliseconds} ms",
                    commandName,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                using (LogContext.PushProperty("ErrorCode", result.Error.Code))
                {
                    logger.LogError(
                        "Completed command {Command} with error ({ErrorCode}) in {ElapsedMilliseconds} ms",
                        commandName,
                        result.Error.Code,
                        stopwatch.ElapsedMilliseconds);
                }
            }

            return result;
        }
    }

    internal sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> innerHandler,
        ILogger<QueryHandler<TQuery, TResponse>> logger)
        : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
        {
            string queryName = typeof(TQuery).Name;

            logger.LogInformation("Processing query {Query}", queryName);

            var stopwatch = Stopwatch.StartNew();
            Result<TResponse> result = await innerHandler.Handle(query, cancellationToken);
            stopwatch.Stop();

            if (result.IsSuccess)
            {
                logger.LogInformation(
                    "Completed query {Query} in {ElapsedMilliseconds} ms",
                    queryName,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                using (LogContext.PushProperty("ErrorCode", result.Error.Code))
                {
                    logger.LogError(
                        "Completed query {Query} with error ({ErrorCode}) in {ElapsedMilliseconds} ms",
                        queryName,
                        result.Error.Code,
                        stopwatch.ElapsedMilliseconds);
                }
            }

            return result;
        }
    }
}
