using Microsoft.AspNetCore.Mvc;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Api.Controllers;

[Route("api/[controller]")]
public class AccountController : ApiControllerBase
{
    private readonly IAccountService _service;

    public AccountController(IAccountService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpPost("query")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<AccountDto>))]
    public Task<IActionResult> Query([FromBody] AccountQueryRequest request)
        => ExecuteAsync(() => _service.GetAccounts(request));

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountDto))]
    public Task<IActionResult> Get(int id)
        => ExecuteAsync(() => _service.GetAccount(id));

    [HttpGet("{id}/availability")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(decimal))]
    public Task<IActionResult> GetAvailability(int id)
        => ExecuteAsync(() => _service.GetAvailability(id));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Post([FromBody] IEnumerable<AccountDto> dto)
        => ExecuteAsync(() => _service.AddAccounts(dto));

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Put([FromBody] AccountDto dto)
        => ExecuteAsync(() => _service.UpdateAccount(dto));
}
