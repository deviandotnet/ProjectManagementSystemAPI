using PMS.Domain.Abstractions;

namespace PMS.Domain.Abstractions.Errors;

public record Error(string Code, string? Description = default, ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty);
    public static readonly Error Null = new("Error.NullValue", "The specified result value is null.");

    public static implicit operator Result(Error error) => Result.Failure(error);

    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    public static Error Unexpected(string code, string description) =>
        new(code, description, ErrorType.Unexpected);

    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);
}

public enum ErrorType
{
    Failure,
    Unexpected,
    Validation,
    Conflict,
    NotFound,
    Unauthorized,
    Forbidden,
    Custom
}
