using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using RDS.ExpenseTracker.Domain.Models;
using RDS.ExpenseTracker.Business.Services.Abstractions;
using RDS.ExpenseTracker.Api.Dtos;
using RDS.ExpenseTracker.Business.QueryFilters;

namespace RDS.ExpenseTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _service;
        private readonly IMapper _mapper;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(ITransactionService service, IMapper mapper, ILogger<TransactionController> logger)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TransactionDto>))]
        public async Task<IResult> Get(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate
            )
        {
            try
            {
                var filter = new TransactionQueryFilter { FromDate = fromDate, ToDate = toDate };
                var results = await _service.GetTransactions(filter);
                var dtos = _mapper.Map<IEnumerable<TransactionDto>>(results);
                return TypedResults.Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching transactions");
                return TypedResults.Problem(ex.Message);
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TransactionDto))]
        public async Task<IResult> Get(int id)
        {
            try
            {
                var transaction = await _service.GetTransaction(id);
                var dto = _mapper.Map<TransactionDto>(transaction);
                return TypedResults.Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching transaction with ID {id}", id);
                return TypedResults.Problem(ex.Message);
            }
        }

        [HttpGet("latest")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TransactionDto))]
        public async Task<IResult> GetLatest()
        {
            try
            {
                var transaction = await _service.GetLatestTransaction();

                if (transaction == null)
                {
                    return TypedResults.NoContent();
                }

                var dto = _mapper.Map<TransactionDto>(transaction);
                return TypedResults.Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching latest transaction");
                return TypedResults.Problem(ex.Message);
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
        public async Task<IResult> Post([FromBody] IEnumerable<TransactionDto> dto)
        {
            try
            {
                var transactions = _mapper.Map<IEnumerable<Transaction>>(dto);
                await _service.AddTransactions(transactions);
                _logger.LogInformation("Transactions created (count: {count})", transactions.Count());
                return TypedResults.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding transactions (count: {count})", dto.Count());
                return TypedResults.Problem($"{ex} {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TransactionDto))]
        public async Task<IResult> Put(int id, [FromBody] TransactionDto dto)
        {
            try
            {
                var transaction = _mapper.Map<Transaction>(dto);
                await _service.UpdateTransaction(transaction);
                _logger.LogInformation("Transaction updated with ID {id}", id);
                return TypedResults.Ok(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating transaction with ID {id}", id);
                return TypedResults.Problem($"{ex} {ex.Message}");
            }
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IResult> Put([FromBody] IEnumerable<TransactionDto> transactionDtos)
        {
            try
            {
                _logger.LogInformation("Received transactions: {transactions}", transactionDtos.Count());
                var transactions = _mapper.Map<IEnumerable<Transaction>>(transactionDtos);
                await _service.ResetTransactions(transactions);
                _logger.LogInformation("Transactions reset successfully");
                return TypedResults.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while resetting transactions");
                return TypedResults.Problem($"{ex} {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IResult> Delete(int id)
        {
            try
            {
                await _service.DeleteTransaction(id);
                _logger.LogInformation("Transaction with ID {id} deleted", id);
                return TypedResults.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting transaction with ID {id}", id);
                return TypedResults.Problem($"{ex} {ex.Message}");
            }
        }
    }
}
