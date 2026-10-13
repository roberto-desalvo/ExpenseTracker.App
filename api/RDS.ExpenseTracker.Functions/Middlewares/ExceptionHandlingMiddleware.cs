using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Domain.Exceptions;
using System.Text.Json;

namespace RDS.ExpenseTracker.Functions.Middlewares;

public class ExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var httpContext = context.GetHttpContext();
            if (httpContext is null)
                throw;

            // Function exceptions may be wrapped by the worker invocation pipeline
            var domainException = ex as DomainException ?? ex.InnerException as DomainException;
            if (domainException is not null)
            {
                _logger.LogWarning(domainException, "Handled domain exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
                await WriteProblemDetailsAsync(httpContext, domainException.StatusCode, domainException.Title, domainException.Message);
                return;
            }

            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            await WriteProblemDetailsAsync(httpContext, StatusCodes.Status500InternalServerError, "An unexpected error occurred.", ex.Message);
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        var json = JsonSerializer.Serialize(problem);
        await context.Response.WriteAsync(json);
    }
}
