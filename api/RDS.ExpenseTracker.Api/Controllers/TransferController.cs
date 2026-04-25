using Microsoft.AspNetCore.Mvc;
using RDS.ExpenseTracker.Api.Dtos;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Api.Controllers;

[Route("api/[controller]")]
public class TransferController : ApiControllerBase
{
    private readonly ITransferService _service;

    public TransferController(ITransferService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TransferDto>))]
    public Task<IActionResult> Get()
        => ExecuteAsync(() => _service.GetTransfers());

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TransferDto))]
    public Task<IActionResult> Get(int id)
        => ExecuteAsync(() => _service.GetTransfer(id));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
    public Task<IActionResult> Post([FromBody] TransferDto dto)
        => ExecuteAsync(() => _service.AddTransfer(dto));

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Put(int id, [FromBody] TransferDto dto)
    {
        dto.Id = id;
        return ExecuteAsync(() => _service.UpdateTransfer(dto));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Delete(int id)
        => ExecuteAsync(() => _service.DeleteTransfer(id));
}