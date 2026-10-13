using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace RDS.ExpenseTracker.Functions.Middlewares;

public class RequestLoggingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext is not null)
        {
            _logger.LogInformation("Received request: {Method} {Url} from {IpAddress} (function {FunctionName})",
                httpContext.Request.Method,
                httpContext.Request.Path,
                httpContext.Connection.RemoteIpAddress?.ToString(),
                context.FunctionDefinition.Name);
        }

        await next(context);
    }
}
