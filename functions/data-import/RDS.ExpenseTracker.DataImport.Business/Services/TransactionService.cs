using AutoMapper;
using RDS.ExpenseTracker.Domain.Models;
using RDS.ExpenseTracker.DataImport.Business.Services.Abstractions;
using RDS.ExpenseTracker.DataImport.DataAccess.Repositories.Abstractions;
using RDS.ExpenseTracker.Api.Dtos;
using Microsoft.Extensions.Logging;

namespace RDS.ExpenseTracker.DataImport.Business.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IMapper _mapper;
        private readonly ITransactionRepository _repository;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(IMapper mapper, ITransactionRepository repository, ILogger<TransactionService> logger)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task AddRangeAsync(IEnumerable<Transaction> transactions)
        {
            try
            {
                var dtos = _mapper.Map<IEnumerable<TransactionDto>>(transactions);
                await _repository.AddAsync(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding transactions (count: {transactions.Count()})", transactions.Count());
                throw;
            }            
        }

        public async Task<IEnumerable<Transaction>> GetAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var transactionDtos = await _repository.GetAsync(fromDate, toDate);
                return _mapper.Map<IEnumerable<Transaction>>(transactionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving transactions from date {fromDate} to date {toDate}", fromDate, toDate);
                throw;
            }
        }

        public async Task<Transaction> GetLatestAsync()
        {
            try
            {
                var transactionDto = await _repository.GetLatestAsync();
                return _mapper.Map<Transaction>(transactionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving latest transaction");
                throw;
            }
        }

        public async Task ResetAllTransactions(IEnumerable<Transaction> transactions)
        {
            try
            {
                var transactionDtos = _mapper.Map<IEnumerable<TransactionDto>>(transactions).ToList();
                await _repository.Reset(transactionDtos);
                _logger.LogInformation("Successfully reset transactions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while resetting transactions");
                throw;
            }
        }
    }
}
