using Microsoft.AspNetCore.Mvc;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Api.Controllers;

[Route("api/[controller]")]
public class DemoController : ApiControllerBase
{
    private readonly IDemoDataService _service;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public DemoController(IDemoDataService service, ICurrentUserAccessor currentUserAccessor)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _currentUserAccessor = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
    }

    [HttpPost("generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Generate()
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GenerateDemoDataAsync(userId));
    }
}
