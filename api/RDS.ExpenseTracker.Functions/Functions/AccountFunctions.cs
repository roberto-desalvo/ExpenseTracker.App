using Microsoft.AspNetCore.Mvc;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Api.Controllers;

[Route("api/[controller]")]
public class AccountController : ApiControllerBase
{
    private readonly IAccountService _service;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public AccountController(IAccountService service, ICurrentUserAccessor currentUserAccessor)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _currentUserAccessor = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
    }

    [HttpPost("query")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<AccountDto>))]
    public async Task<IActionResult> Query([FromBody] AccountQueryRequest request)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetAccounts(request, userId));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountDto))]
    public async Task<IActionResult> Get(int id)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetAccount(id, userId));
    }

    [HttpGet("{id}/availability")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(decimal))]
    public async Task<IActionResult> GetAvailability(int id)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetAvailability(id, userId));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Post([FromBody] IEnumerable<AccountDto> dto)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.AddAccounts(dto, userId));
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Put([FromBody] AccountDto dto)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.UpdateAccount(dto, userId));
    }
}
