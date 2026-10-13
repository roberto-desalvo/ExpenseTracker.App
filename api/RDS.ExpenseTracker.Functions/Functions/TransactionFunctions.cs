using Microsoft.AspNetCore.Mvc;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Api.Controllers;

[Route("api/[controller]")]
public class TransactionController : ApiControllerBase
{
    private readonly ITransactionService _service;

    public TransactionController(ITransactionService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpPost("query")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TransactionQueryResult))]
    public Task<IActionResult> Query([FromBody] TransactionQueryRequest request)
        => ExecuteAsync(() => _service.GetPagedTransactions(request));

    [HttpGet("month-options")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TransactionMonthOptionDto>))]
    public Task<IActionResult> GetMonthOptions()
        => ExecuteAsync(() => _service.GetAvailableMonthOptions());

    [HttpGet("landing")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LandingDashboardDto))]
    public Task<IActionResult> GetLanding([FromQuery] bool excludeTransfers = true)
        => ExecuteAsync(() => _service.GetLanding(excludeTransfers));

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TransactionDto))]
    public Task<IActionResult> Get(int id)
        => ExecuteAsync(() => _service.GetTransaction(id));

    [HttpGet("latest")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TransactionDto))]
    public Task<IActionResult> GetLatest()
        => ExecuteAsync(() => _service.GetLatestTransaction());

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Post([FromBody] TransactionDto dto)
        => ExecuteAsync(() => _service.AddTransaction(dto));

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Put(int id, [FromBody] TransactionDto dto)
        => ExecuteAsync(() => _service.UpdateTransaction(dto));

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Delete(int id)
        => ExecuteAsync(() => _service.DeleteTransaction(id));

    [HttpPost("series")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TimeSeriesListDto))]
    public Task<IActionResult> GetTimeSeries([FromBody] TimeSeriesRequestDto request)
    => ExecuteAsync(() => _service.GetTimeSeries(request));

    [HttpPost("stock")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TimeSeriesListDto))]
    public Task<IActionResult> GetStock([FromBody] TimeSeriesRequestDto request)
    => ExecuteAsync(() => _service.GetStock(request));
}
