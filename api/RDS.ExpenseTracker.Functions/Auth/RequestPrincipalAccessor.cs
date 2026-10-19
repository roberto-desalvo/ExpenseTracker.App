using System.Security.Claims;

namespace RDS.ExpenseTracker.Functions.Auth;

/// <summary>
/// Holds the authenticated principal of the current function invocation (scoped per invocation).
/// Populated by <see cref="Middlewares.AuthenticationMiddleware"/>.
/// </summary>
public class RequestPrincipalAccessor
{
    public ClaimsPrincipal? Principal { get; set; }
}
