namespace RDS.ExpenseTracker.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public int StatusCode { get; }
    public string Title { get; }

    protected DomainException(string message, int statusCode, string title)
        : base(message)
    {
        StatusCode = statusCode;
        Title = title;
    }
}

public sealed class BadRequestDomainException(string message)
    : DomainException(message, 400, "Bad Request");

public sealed class ValidationDomainException(string message)
    : DomainException(message, 422, "Validation Error");

public sealed class UnauthorizedDomainException(string message)
    : DomainException(message, 401, "Unauthorized");

public sealed class ForbiddenDomainException(string message)
    : DomainException(message, 403, "Forbidden");

public sealed class NotFoundDomainException(string message)
    : DomainException(message, 404, "Not Found");

public sealed class ConflictDomainException(string message)
    : DomainException(message, 409, "Conflict");