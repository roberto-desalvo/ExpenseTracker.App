using FluentResults;
using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Domain.Common;

public abstract class DomainResultError : Error
{
    public DomainErrorKind Kind { get; }
    public string Code { get; }

    protected DomainResultError(DomainErrorKind kind, string code, string message)
        : base(message)
    {
        Kind = kind;
        Code = code;
        Metadata[nameof(Code)] = code;
        Metadata[nameof(Kind)] = kind.ToString();
    }
}

public sealed class BadRequestResultError(string code, string message)
    : DomainResultError(DomainErrorKind.BadRequest, code, message);

public sealed class ValidationResultError(string code, string message)
    : DomainResultError(DomainErrorKind.Validation, code, message);

public sealed class UnauthorizedResultError(string code, string message)
    : DomainResultError(DomainErrorKind.Unauthorized, code, message);

public sealed class ForbiddenResultError(string code, string message)
    : DomainResultError(DomainErrorKind.Forbidden, code, message);

public sealed class NotFoundResultError(string code, string message)
    : DomainResultError(DomainErrorKind.NotFound, code, message);

public sealed class ConflictResultError(string code, string message)
    : DomainResultError(DomainErrorKind.Conflict, code, message);

public static class DomainErrors
{
    public static ValidationResultError InvalidId(string entityName, int id)
        => new("validation.invalid_id", $"Invalid {entityName} id: {id}.");

    public static NotFoundResultError NotFound(string entityName, int id)
        => new("not_found", $"{entityName} with id '{id}' not found.");

    public static NotFoundResultError NotFound(string entityName)
        => new("not_found", $"{entityName} not found.");

    public static ValidationResultError Required(string fieldName)
        => new("validation.required", $"{fieldName} is required.");

    public static ConflictResultError Conflict(string message)
        => new("conflict", message);

    public static UnauthorizedResultError Unauthorized(string message)
        => new("unauthorized", message);

    public static BadRequestResultError BadRequest(string message)
        => new("bad_request", message);
}
