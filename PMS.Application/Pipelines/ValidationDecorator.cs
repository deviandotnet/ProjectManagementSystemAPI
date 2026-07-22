using FluentValidation;
using FluentValidation.Results;
using PMS.Application.Abstractions;
using PMS.Domain.Abstractions;
using PMS.Domain.Abstractions.Errors;

namespace PMS.Application.Pipelines;

public sealed class ValidationDecorator<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    IHandler<TRequest, TResponse> innerHandler) : IHandler<TRequest, TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest command, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await innerHandler.HandleAsync(command, cancellationToken);
        }

        var context = new ValidationContext<TRequest>(command);

        ValidationFailure[] failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToArray();

        if (failures.Length == 0)
        {
            return await innerHandler.HandleAsync(command, cancellationToken);
        }

        return CreateFailureResponse(failures);
    }

    private static TResponse CreateFailureResponse(ValidationFailure[] failures)
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(CreateValidationError(failures));
        }

        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = typeof(TResponse).GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethods()
                .First(m =>
                    m.Name == nameof(Result.Failure) &&
                    m.IsGenericMethodDefinition &&
                    m.GetParameters().Length == 1);

            var typedFailure = failureMethod
                .MakeGenericMethod(valueType)
                .Invoke(null, [CreateValidationError(failures)]);

            return (TResponse)typedFailure!;
        }

        throw new InvalidOperationException(
            $"ValidationDecorator supports only {nameof(Result)} and {nameof(Result<object>)} responses. " +
            $"Received {typeof(TResponse).FullName}.");
    }

    private static ValidationError CreateValidationError(ValidationFailure[] validationFailures) =>
       new(validationFailures.Select(f => Error.Validation(f.ErrorCode, f.ErrorMessage)).ToArray());
}
