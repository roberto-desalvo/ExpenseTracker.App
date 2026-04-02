using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using RDS.ExpenseTracker.Domain.Models;
using RDS.ExpenseTracker.Business.Services.Abstractions;
using RDS.ExpenseTracker.Api.Dtos;
using Microsoft.AspNetCore.Authorization;


namespace RDS.ExpenseTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IFinancialAccountService _service;
        private readonly IMapper _mapper;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IFinancialAccountService service, IMapper mapper, ILogger<AccountController> logger)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FinancialAccountDto>))]
        public async Task<IResult> Get()
        {
            try
            {
                var results = await _service.GetFinancialAccounts();
                var dtos = _mapper.Map<IEnumerable<FinancialAccountDto>>(results);
                return TypedResults.Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching accounts");
                return TypedResults.Problem(ex.Message);
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FinancialAccountDto))]
        public async Task<IResult> Get(int id)
        {
            try
            {
                var account = await _service.GetFinancialAccount(id);
                var dto = _mapper.Map<FinancialAccountDto>(account);
                return TypedResults.Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching account with ID {id}", id);
                return TypedResults.Problem(ex.Message);
            }
        }

        [HttpGet("{id}/availability")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
        public async Task<IResult> GetAvailability(int id)
        {
            try
            {
                var availability = await _service.GetAvailability(id);
                return TypedResults.Ok(availability);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching availability for account with ID {id}", id);
                return TypedResults.Problem(ex.Message);
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IResult> Post([FromBody] IEnumerable<FinancialAccountDto> dto)
        {
            try
            {
                var accounts = _mapper.Map<IEnumerable<FinancialAccount>>(dto);
                await _service.AddFinancialAccounts(accounts);
                
                _logger.LogInformation("Accounts created: {accounts} ", string.Join(" - ", accounts.Select(x => $"Id: {x.Id}, Name: {x.Name}")));
                return TypedResults.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating accounts: {accounts}", string.Join(" - ", dto.Select(x => x.Name)));
                return TypedResults.Problem($"{ex} {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FinancialAccountDto))]
        public async Task<IResult> Put(int id, [FromBody] FinancialAccountDto dto)
        {            
            var account = _mapper.Map<FinancialAccount>(dto);

            try
            {
                await _service.UpdateFinancialAccount(account);
                _logger.LogInformation("Account {account.Name} updated with ID {id}", account.Name, account.Id);
                return TypedResults.Ok(account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account {account.Name}", account.Name);
                return TypedResults.Problem($"{ex} {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IResult> Delete(int id)
        {
            try
            {
                await _service.DeleteFinancialAccount(id);
                _logger.LogInformation("Account with ID {id} deleted", id);
                return TypedResults.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account with ID {id}", id);
                return TypedResults.Problem($"{ex} {ex.Message}");
            }
        }
    }
}
