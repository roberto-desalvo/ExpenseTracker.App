using Microsoft.AspNetCore.Mvc;
using RDS.ExpenseTracker.Api.Dtos;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Api.Controllers
{
    [Route("api/[controller]")]
    public class TransactionController : ApiControllerBase
    {
        private readonly ITransactionService _service;

        public TransactionController(ITransactionService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TransactionDto>))]
        public Task<IActionResult> Get(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            var filter = new TransactionQueryRequest { FromDate = fromDate, ToDate = toDate };
            return ExecuteAsync(() => _service.GetTransactions(filter));
        }

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
        public Task<IActionResult> Post([FromBody] IEnumerable<TransactionDto> dto)
            => ExecuteAsync(() => _service.AddTransactions(dto));

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Task<IActionResult> Put(int id, [FromBody] TransactionDto dto)
            => ExecuteAsync(() => _service.UpdateTransaction(dto));

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Task<IActionResult> Put([FromBody] IEnumerable<TransactionDto> dtos)
            => ExecuteAsync(() => _service.ResetTransactions(dtos));

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Task<IActionResult> Delete(int id)
            => ExecuteAsync(() => _service.DeleteTransaction(id));
    }
}
