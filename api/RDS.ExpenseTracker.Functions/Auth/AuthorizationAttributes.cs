namespace RDS.ExpenseTracker.Functions.Auth;

/// <summary>
/// Requires the caller to have at least one of the given app roles.
/// Can be applied to a functions class or to a single function method.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireRoleAttribute(params string[] roles) : Attribute
{
    public IReadOnlyList<string> Roles { get; } = roles;
}

/// <summary>
/// Skips bearer token validation for the decorated function.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowAnonymousFunctionAttribute : Attribute;
