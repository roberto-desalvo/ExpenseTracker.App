using System.Security.Claims;
using Microsoft.Identity.Web;
using RDS.ExpenseTracker.Domain.Exceptions;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Api.Auth;

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserService _userService;
    private int? _cachedUserId;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor, IUserService userService)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    public async Task<int> GetUserIdAsync()
    {
        if (_cachedUserId.HasValue)
            return _cachedUserId.Value;

        var principal = _httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedDomainException("No authenticated user in current request.");

        var oid = principal.GetObjectId();
        if (string.IsNullOrEmpty(oid))
            throw new UnauthorizedDomainException("Token does not contain an oid claim.");

        var email = principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("preferred_username")?.Value
            ?? throw new UnauthorizedDomainException("Token does not contain an email claim.");

        var name = principal.FindFirst(ClaimTypes.Name)?.Value;

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

        var principal = _httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedDomainException("No authenticated user in current request.");

        var oid = principal.GetObjectId();
        if (string.IsNullOrEmpty(oid))
            throw new UnauthorizedDomainException("Token does not contain an oid claim.");

        var email = principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("preferred_username")?.Value;

        var name = principal.FindFirst(ClaimTypes.Name)?.Value;

        var result = await _userService.GetOrCreateUserForImportAsync(oid, email, name);
        if (result.IsFailed)
            throw new UnauthorizedDomainException(string.Join("; ", result.Errors.Select(e => e.Message)));

        _cachedUserId = result.Value.Id;
        return _cachedUserId.Value;
    }
}
