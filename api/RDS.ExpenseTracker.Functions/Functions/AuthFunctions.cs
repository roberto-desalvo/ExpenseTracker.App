using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Exceptions;
using RDS.ExpenseTracker.Domain.Services;
using RDS.ExpenseTracker.Functions.Auth;

namespace RDS.ExpenseTracker.Functions.Functions;

public class AuthFunctions
{
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IUserService _userService;
    private readonly DemoTokenService _demoTokenService;
    private readonly ILogger<AuthFunctions> _logger;

    public AuthFunctions(
        ICurrentUserAccessor currentUserAccessor,
        IUserService userService,
        DemoTokenService demoTokenService,
        ILogger<AuthFunctions> logger)
    {
        _currentUserAccessor = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _demoTokenService = demoTokenService ?? throw new ArgumentNullException(nameof(demoTokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

    /// <summary>
    /// Demo login: issues a short-lived token for the test user (looked up by email, never created here:
    /// the user and its data come from <c>Import_TestSeed</c>). 404 when <c>DemoAuth:Enabled</c> is false.
    /// </summary>
    [AllowAnonymousFunction]
    [Function("Auth_DemoLogin")]
    public async Task<IActionResult> DemoLogin(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/demo")] HttpRequest req)
    {
        if (!_demoTokenService.IsEnabled)
            throw new NotFoundDomainException("Demo login is not enabled.");

        var user = await _userService.GetUserByEmailAsync(TestSeedService.TestUserEmail);
        if (user.IsFailed)
        {
            _logger.LogWarning("Demo login requested but the test user {Email} does not exist", TestSeedService.TestUserEmail);
            throw new NotFoundDomainException("La demo non è al momento disponibile.");
        }

        var (accessToken, expiresOn) = _demoTokenService.CreateToken(user.Value.Email);
        _logger.LogInformation("Demo token issued for user {UserId}", user.Value.Id);

        return new OkObjectResult(new
        {
            AccessToken = accessToken,
            ExpiresOn = expiresOn,
            UserName = DemoTokenService.DemoUserName,
        });
    }
}
