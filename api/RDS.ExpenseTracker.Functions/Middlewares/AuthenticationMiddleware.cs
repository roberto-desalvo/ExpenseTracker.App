using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Domain.Exceptions;
using RDS.ExpenseTracker.Functions.Auth;

namespace RDS.ExpenseTracker.Functions.Middlewares;

/// <summary>
/// Validates the bearer token of HTTP-triggered functions and enforces <see cref="RequireRoleAttribute"/>.
/// Every HTTP function requires an authenticated user unless marked with <see cref="AllowAnonymousFunctionAttribute"/>.
/// </summary>
public class AuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    private static readonly ConcurrentDictionary<string, FunctionAuthMetadata> MetadataCache = new();

    private readonly JwtTokenValidator _tokenValidator;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(JwtTokenValidator tokenValidator, ILogger<AuthenticationMiddleware> logger)
    {
        _tokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(tokenValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        var metadata = MetadataCache.GetOrAdd(context.FunctionDefinition.EntryPoint, ResolveMetadata);

        if (httpContext is null || metadata.AllowAnonymous)
        {
            await next(context);
            return;
        }

        var token = GetBearerToken(httpContext.Request.Headers.Authorization.ToString())
            ?? throw new UnauthorizedDomainException("Missing bearer token.");

        ClaimsPrincipal? principal;
        try
        {
            principal = await _tokenValidator.ValidateAsync(token, context.CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Token validation failed for function {FunctionName}", context.FunctionDefinition.Name);
            throw new UnauthorizedDomainException("Unable to validate bearer token.");
        }

        if (principal is null)
            throw new UnauthorizedDomainException("Invalid bearer token.");

        if (metadata.RequiredRoles.Count > 0 && !metadata.RequiredRoles.Any(role => HasRole(principal, role)))
            throw new ForbiddenDomainException("The caller does not have the required role.");

        httpContext.User = principal;
        context.InstanceServices.GetRequiredService<RequestPrincipalAccessor>().Principal = principal;

        await next(context);
    }

    private static string? GetBearerToken(string authorizationHeader)
    {
        const string prefix = "Bearer ";
        if (string.IsNullOrWhiteSpace(authorizationHeader)
            || !authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var token = authorizationHeader[prefix.Length..].Trim();
        return string.IsNullOrEmpty(token) ? null : token;
    }

    private static bool HasRole(ClaimsPrincipal principal, string role)
        => principal.IsInRole(role) || principal.HasClaim("roles", role);

    private static FunctionAuthMetadata ResolveMetadata(string entryPoint)
    {
        // EntryPoint format: "Namespace.TypeName.MethodName"
        var separatorIndex = entryPoint.LastIndexOf('.');
        var type = separatorIndex > 0
            ? typeof(AuthenticationMiddleware).Assembly.GetType(entryPoint[..separatorIndex])
            : null;
        var method = type?.GetMethod(entryPoint[(separatorIndex + 1)..], BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        if (type is null || method is null)
            return new FunctionAuthMetadata(false, []);

        var allowAnonymous = method.IsDefined(typeof(AllowAnonymousFunctionAttribute), true)
            || type.IsDefined(typeof(AllowAnonymousFunctionAttribute), true);

        var roles = type.GetCustomAttributes<RequireRoleAttribute>(true)
            .Concat(method.GetCustomAttributes<RequireRoleAttribute>(true))
            .SelectMany(a => a.Roles)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new FunctionAuthMetadata(allowAnonymous, roles);
    }

    private sealed record FunctionAuthMetadata(bool AllowAnonymous, IReadOnlyList<string> RequiredRoles);
}
