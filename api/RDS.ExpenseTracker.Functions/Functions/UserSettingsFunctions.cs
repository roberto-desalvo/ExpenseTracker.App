using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Functions.Functions;

/// <summary>Profilo dell'utente corrente e preferenze salvate a DB (layout della pagina Analisi).</summary>
public class UserSettingsFunctions : FunctionBase
{
    private readonly IUserService _service;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public UserSettingsFunctions(IUserService service, ICurrentUserAccessor currentUserAccessor)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _currentUserAccessor = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
    }

    [Function("UserSettings_GetMe")]
    public async Task<IActionResult> GetMe(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/me")] HttpRequest req)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetCurrentUserAsync(userId));
    }

    [Function("UserSettings_GetAnalyticsLayout")]
    public async Task<IActionResult> GetAnalyticsLayout(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/analytics-layout")] HttpRequest req)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetAnalyticsLayoutAsync(userId));
    }

    [Function("UserSettings_PutAnalyticsLayout")]
    public async Task<IActionResult> PutAnalyticsLayout(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "user/analytics-layout")] HttpRequest req)
    {
        var layout = await ReadBodyAsync<AnalyticsLayoutDto>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.SaveAnalyticsLayoutAsync(userId, layout));
    }
}
