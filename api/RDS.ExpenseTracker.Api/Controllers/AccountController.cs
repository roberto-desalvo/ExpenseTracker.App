using Microsoft.AspNetCore.Mvc;
using RDS.ExpenseTracker.Api.Dtos;
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

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FinancialAccountDto>))]
    public Task<IActionResult> Get()
        => ExecuteAsync(() => _service.GetAccounts());

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FinancialAccountDto))]
    public Task<IActionResult> Get(int id)
        => ExecuteAsync(() => _service.GetAccount(id));

    [HttpGet("{id}/availability")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(decimal))]
    public Task<IActionResult> GetAvailability(int id)
        => ExecuteAsync(() => _service.GetAvailability(id));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Post([FromBody] IEnumerable<FinancialAccountDto> dto)
        => ExecuteAsync(() => _service.AddAccounts(dto));

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Put(int id, [FromBody] FinancialAccountDto dto)
        => ExecuteAsync(() => _service.UpdateAccount(dto));

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Delete(int id)
        => ExecuteAsync(() => _service.DeleteAccount(id));
}
