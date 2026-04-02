using AutoMapper;
using RDS.ExpenseTracker.Domain.Models;
using RDS.ExpenseTracker.DataImport.Business.Services.Abstractions;
using System.Linq;
using RDS.ExpenseTracker.DataImport.DataAccess.Repositories.Abstractions;
using RDS.ExpenseTracker.Api.Dtos;
using Microsoft.Extensions.Logging;

namespace RDS.ExpenseTracker.DataImport.Business.Services
{
    public class FinancialAccountService : IFinancialAccountService
    {
        private readonly IMapper _mapper;
        private readonly IRepository<FinancialAccountDto> _repository;
        private readonly ILogger<FinancialAccountService> _logger;

        public FinancialAccountService(IMapper mapper, IRepository<FinancialAccountDto> repository, ILogger<FinancialAccountService> logger)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<int> AddFinancialAccount(FinancialAccount account)
        {
            try
            {
                var item = _mapper.Map<FinancialAccountDto>(account);
                await _repository.AddAsync(new List<FinancialAccountDto> { item });
                _logger.LogInformation("Successfully added financial account with name {name}", item.Name);
                return item.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding financial account with name {name}", account.Name);
                throw;
            }
        }

        public async Task<IEnumerable<FinancialAccount>> GetFinancialAccounts()
        {
            try
            {
                var result = await _repository.GetAllAsync();
                var accounts = _mapper.Map<IEnumerable<FinancialAccount>>(result);
                _logger.LogInformation("Successfully fetched financial accounts");
                return accounts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching financial accounts");
                throw;
            }
        }
    }
}
