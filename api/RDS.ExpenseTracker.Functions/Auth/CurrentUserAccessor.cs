using System.Security.Claims;
using RDS.ExpenseTracker.Domain.Exceptions;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Functions.Auth;

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly RequestPrincipalAccessor _principalAccessor;
    private readonly IUserService _userService;
    private int? _cachedUserId;

    public CurrentUserAccessor(RequestPrincipalAccessor principalAccessor, IUserService userService)
    {
        _principalAccessor = principalAccessor ?? throw new ArgumentNullException(nameof(principalAccessor));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    public async Task<int> GetUserIdAsync()
    {
        if (_cachedUserId.HasValue)
            return _cachedUserId.Value;

        var principal = _principalAccessor.Principal
            ?? throw new UnauthorizedDomainException("No authenticated user in current request.");

        var oid = GetObjectId(principal);
        if (string.IsNullOrEmpty(oid))
            throw new UnauthorizedDomainException("Token does not contain an oid claim.");

        var email = principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value
            ?? principal.FindFirst("preferred_username")?.Value
            ?? throw new UnauthorizedDomainException("Token does not contain an email claim.");

        var name = principal.FindFirst(ClaimTypes.Name)?.Value ?? principal.FindFirst("name")?.Value;

        var result = await _userService.GetOrCreateUserAsync(oid, email, name);
        if (result.IsFailed)
            throw new UnauthorizedDomainException(string.Join("; ", result.Errors.Select(e => e.Message)));

        _cachedUserId = result.Value.Id;
        return _cachedUserId.Value;
    }

    public async Task<int> GetUserIdForImportAsync()
    {
        if (_cachedUserId.HasValue)
            return _cachedUserId.Value;

        var principal = _principalAccessor.Principal
            ?? throw new UnauthorizedDomainException("No authenticated user in current request.");

        var oid = GetObjectId(principal);
        if (string.IsNullOrEmpty(oid))
            throw new UnauthorizedDomainException("Token does not contain an oid claim.");

        var email = principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value
            ?? principal.FindFirst("preferred_username")?.Value;

        var name = principal.FindFirst(ClaimTypes.Name)?.Value ?? principal.FindFirst("name")?.Value;

        var result = await _userService.GetOrCreateUserForImportAsync(oid, email, name);
        if (result.IsFailed)
            throw new UnauthorizedDomainException(string.Join("; ", result.Errors.Select(e => e.Message)));

        _cachedUserId = result.Value.Id;
        return _cachedUserId.Value;
    }

    private static string? GetObjectId(ClaimsPrincipal principal)
        => principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? principal.FindFirst("oid")?.Value;
}
