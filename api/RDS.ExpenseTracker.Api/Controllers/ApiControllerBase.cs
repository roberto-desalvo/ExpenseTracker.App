using FluentResults;
using Microsoft.AspNetCore.Mvc;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Exceptions;

namespace RDS.ExpenseTracker.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected async Task<IActionResult> ExecuteAsync<T>(Func<Task<Result<T>>> action)
    {
        var result = await action();
        if (result.IsFailed)
        {
            ThrowForResultFailure(result.Errors);
        }
        return Ok(result.Value);
    }

    protected async Task<IActionResult> ExecuteAsync(Func<Task<Result>> action)
    {
        var result = await action();
        if (result.IsFailed)
        {
            ThrowForResultFailure(result.Errors);
        }
        return Ok();
    }

    private static void ThrowForResultFailure(IEnumerable<IError> errors)
    {
        var list = errors?.ToList() ?? [];
        var domainError = list.OfType<DomainResultError>().FirstOrDefault();

        if (domainError is not null)
        {
            throw domainError.Kind switch
            {
                DomainErrorKind.NotFound => new NotFoundDomainException(domainError.Message),
                DomainErrorKind.Conflict => new ConflictDomainException(domainError.Message),
                DomainErrorKind.Validation => new ValidationDomainException(domainError.Message),
                DomainErrorKind.Unauthorized => new UnauthorizedDomainException(domainError.Message),
                DomainErrorKind.Forbidden => new ForbiddenDomainException(domainError.Message),
                _ => new BadRequestDomainException(domainError.Message)
            };
        }

        var messages = list.Select(e => e.Message).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var message = string.Join("; ", messages);
        throw new BadRequestDomainException(string.IsNullOrWhiteSpace(message) ? "Request failed." : message);
    }
}
