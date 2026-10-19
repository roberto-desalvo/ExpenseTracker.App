using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Functions.Functions;

public class AuthFunctions
{
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public AuthFunctions(ICurrentUserAccessor currentUserAccessor)
    {
        _currentUserAccessor = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
    }

    [Function("Auth_GetProfile")]
    public async Task<IActionResult> GetProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth")] HttpRequest req)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();

        return new OkObjectResult(new
        {
            UserId = userId
        });
    }
}
