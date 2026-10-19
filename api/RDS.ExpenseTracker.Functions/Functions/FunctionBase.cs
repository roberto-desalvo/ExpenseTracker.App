using System.Globalization;
using System.Text.Json;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Exceptions;

namespace RDS.ExpenseTracker.Functions.Functions;

public abstract class FunctionBase
{
    protected static async Task<IActionResult> ExecuteAsync<T>(Func<Task<Result<T>>> action)
    {
        var result = await action();
        if (result.IsFailed)
        {
            ThrowForResultFailure(result.Errors);
        }
        return new OkObjectResult(result.Value);
    }

    protected static async Task<IActionResult> ExecuteAsync(Func<Task<Result>> action)
    {
        var result = await action();
        if (result.IsFailed)
        {
            ThrowForResultFailure(result.Errors);
        }
        return new OkResult();
    }

    /// <summary>
    /// Deserializes the JSON request body using the ASP.NET Core web defaults (camelCase, case-insensitive).
    /// </summary>
    protected static async Task<T> ReadBodyAsync<T>(HttpRequest request)
    {
        try
        {
            var body = await request.ReadFromJsonAsync<T>(request.HttpContext.RequestAborted);
            return body ?? throw new BadRequestDomainException("Request body is required.");
        }
        catch (JsonException ex)
        {
            throw new BadRequestDomainException($"Invalid request body: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            // Thrown when the content type is not JSON
            throw new BadRequestDomainException(ex.Message);
        }
    }

    protected static bool GetQueryBool(HttpRequest request, string name, bool defaultValue)
    {
        var raw = request.Query[name].ToString();
        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;

        return bool.TryParse(raw, out var value)
            ? value
            : throw new BadRequestDomainException($"Query parameter '{name}' must be a boolean.");
    }

    protected static DateTime? GetQueryDate(HttpRequest request, string name)
    {
        var raw = request.Query[name].ToString();
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
            ? value
            : throw new BadRequestDomainException($"Query parameter '{name}' must be a date (e.g. 2026-10-01).");
    }

    protected static string GetQueryString(HttpRequest request, string name, string defaultValue)
    {
        var raw = request.Query[name].ToString();
        return string.IsNullOrWhiteSpace(raw) ? defaultValue : raw;
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
