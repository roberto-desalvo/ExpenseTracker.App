using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RDS.ExpenseTracker.Domain.Exceptions;
using RDS.ExpenseTracker.Functions.Auth;
using RDS.ExpenseTracker.Functions.Options;

namespace RDS.ExpenseTracker.Functions.Middlewares;

/// <summary>
/// Validates the bearer token of HTTP-triggered functions and enforces <see cref="RequireRoleAttribute"/>.
/// Every HTTP function requires an authenticated user unless marked with <see cref="AllowAnonymousFunctionAttribute"/>.
/// </summary>
public class AuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    private static readonly ConcurrentDictionary<string, FunctionAuthMetadata> MetadataCache = new();

    /// <summary>Authentication type of the principals created by the local dev bypass.</summary>
    public const string DevAuthenticationType = "DevUser";

    private const string DevUserScheme = "DevUser ";
    private const int DevUserMaxLength = 60;

    private readonly JwtTokenValidator _tokenValidator;
    private readonly DevAuthOptions _devAuthOptions;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(
        JwtTokenValidator tokenValidator,
        IOptions<DevAuthOptions> devAuthOptions,
        ILogger<AuthenticationMiddleware> logger)
    {
        _tokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(tokenValidator));
        _devAuthOptions = devAuthOptions?.Value ?? throw new ArgumentNullException(nameof(devAuthOptions));
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

        var authorizationHeader = httpContext.Request.Headers.Authorization.ToString();

        if (_devAuthOptions.Enabled
            && authorizationHeader.StartsWith(DevUserScheme, StringComparison.OrdinalIgnoreCase))
        {
            // Local development only (enforced at startup): trust the username and skip role checks.
            var devPrincipal = CreateDevPrincipal(authorizationHeader[DevUserScheme.Length..]);
            httpContext.User = devPrincipal;
            context.InstanceServices.GetRequiredService<RequestPrincipalAccessor>().Principal = devPrincipal;

            await next(context);
            return;
        }

        var token = GetBearerToken(authorizationHeader)
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

    private static ClaimsPrincipal CreateDevPrincipal(string rawUserName)
    {
        var userName = rawUserName.Trim();
        if (userName.Length == 0 || userName.Length > DevUserMaxLength)
            throw new UnauthorizedDomainException($"Dev username must be between 1 and {DevUserMaxLength} characters.");

        var normalized = userName.ToLowerInvariant();
        var email = normalized.Contains('@') ? normalized : $"{normalized}@dev.local";

        var identity = new ClaimsIdentity(
            [
                new Claim("oid", $"dev:{normalized}"),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, userName)
            ],
            authenticationType: DevAuthenticationType,
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role);

        return new ClaimsPrincipal(identity);
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
