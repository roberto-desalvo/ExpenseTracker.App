using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Functions.Functions;

public class DemoFunctions : FunctionBase
{
    private readonly IDemoDataService _service;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public DemoFunctions(IDemoDataService service, ICurrentUserAccessor currentUserAccessor)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _currentUserAccessor = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
    }

    [Function("Demo_Generate")]
    public async Task<IActionResult> Generate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "demo/generate")] HttpRequest req)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GenerateDemoDataAsync(userId));
    }
}
