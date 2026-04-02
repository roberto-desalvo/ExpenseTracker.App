using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.Domain.Models;
using RDS.ExpenseTracker.Business.Services.Abstractions;
using RDS.ExpenseTracker.DataAccess;
using Entities = RDS.ExpenseTracker.DataAccess.Entities;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Business.QueryFilters;

namespace RDS.ExpenseTracker.Business.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IMapper _mapper;
        private readonly ExpenseTrackerContext _context;
        private readonly IFinancialAccountService _accountService;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(IMapper mapper, ExpenseTrackerContext context, IFinancialAccountService accountService, ILogger<TransactionService> logger)
        {
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task AddTransactions(IEnumerable<Transaction> transactions)
        {
            var entites = _mapper.Map<IEnumerable<Entities.Transaction>>(transactions);
            await _context.Transactions.AddRangeAsync(entites);
            foreach (var transaction in entites)
            {
                await _accountService.UpdateAvailability(transaction.FinancialAccountId, transaction.Amount, false);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<int> AddTransaction(Transaction transaction)
        {
            return await AddTransaction(transaction, true);
        }

        public async Task<int> AddTransaction(Transaction transaction, bool saveChanges)
        {
            var entity = _mapper.Map<DataAccess.Entities.Transaction>(transaction);

            await _context.Transactions.AddAsync(entity);
            await _accountService.UpdateAvailability(entity.FinancialAccountId, entity.Amount, false);

            if (saveChanges)
            {
                await _context.SaveChangesAsync();
            }

            return entity.Id;
        }

        public async Task DeleteTransaction(int id)
        {
            var entity = await _context.Transactions.FirstOrDefaultAsync(x => x.Id == id);
            if (entity != null)
            {
                _context.Transactions.Remove(entity);
                await _accountService.UpdateAvailability(entity.FinancialAccountId, -entity.Amount, false);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Transaction?> GetTransaction(int id)
        {
            var entity = await _context.Transactions.FirstOrDefaultAsync(x => x.Id == id);
            return _mapper.Map<Transaction?>(entity);
        }

        public async Task<Transaction> GetLatestTransaction()
        {
            var entity = await _context.Transactions.OrderByDescending(x => x.Date).FirstOrDefaultAsync();
            return _mapper.Map<Transaction>(entity);
        }

        public async Task UpdateTransaction(Transaction modified)
        {
            var current = await _context.Transactions.FirstOrDefaultAsync(x => x.Id == modified.Id);
            if (current != null)
            {
                if (current.Amount != modified.Amount)
                {
                    current.Amount = modified.Amount;

                    var delta = modified.Amount - current.Amount;

                    await _accountService.UpdateAvailability(current.FinancialAccountId, delta, false);
                }
                current.Description = modified.Description;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Transaction>> GetTransactions()
        {
            return await GetTransactions(null);
        }

        public async Task<IEnumerable<Transaction>> GetTransactions(TransactionQueryFilter? filter)
        {
            var query = _context.Transactions.AsQueryable();


            if (filter != null)
            {
                if (filter.FromDate != null)
                {
                    query = query.Where(x => x.Date >= filter.FromDate);
                }

                if (filter.ToDate != null)
                {
                    query = query.Where(x => x.Date <= filter.ToDate);
                }
            }

            query = query.Include(x => x.FinancialAccount);
            query = query.Include(x => x.Category);

            var results = await query.ToListAsync();

            return _mapper.Map<IEnumerable<Transaction>>(results);
        }

        public async Task DeleteAllTransactions()
        {
            try
            {
                var transactions = await _context.Transactions.ToListAsync();
                _context.Transactions.RemoveRange(transactions);
                foreach (var transaction in transactions)
                {
                    await _accountService.UpdateAvailability(transaction.FinancialAccountId, -transaction.Amount, false);
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("All transactions deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting all transactions");
                throw;
            }
        }

        public async Task ResetTransactions(IEnumerable<Transaction> transactions)
        {
            await DeleteAllTransactions();
            var accounts = await _accountService.GetFinancialAccounts();
            foreach (var account in accounts)
            {
                await _accountService.UpdateAvailability(account.Id, 0, false);
            }
            _logger.LogInformation("Availabilities updated successfully");
            await AddTransactions(transactions);
        }


    }
}
