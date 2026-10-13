using Microsoft.AspNetCore.Mvc;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public AuthController(ICurrentUserAccessor currentUserAccessor)
    {
        _currentUserAccessor = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
    }

    [HttpGet]
    public async Task<IResult> GetProfile()
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();

        return TypedResults.Ok(new
        {
            UserId = userId
        });
    }
}
