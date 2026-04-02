using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.Domain.Models;
using RDS.ExpenseTracker.Business.Services.Abstractions;
using RDS.ExpenseTracker.DataAccess;
using Entities = RDS.ExpenseTracker.DataAccess.Entities;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace RDS.ExpenseTracker.Business.Services
{
    public class FinancialAccountService : IFinancialAccountService
    {
        private readonly IMapper _mapper;
        private readonly ExpenseTrackerContext _context;
        private readonly ILogger<FinancialAccountService> _logger;

        public FinancialAccountService(IMapper mapper, ExpenseTrackerContext context, ILogger<FinancialAccountService> logger)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task AddFinancialAccounts(IEnumerable<FinancialAccount> accounts)
        {
            var items = _mapper.Map<IEnumerable<Entities.FinancialAccount>>(accounts);
            await _context.FinancialAccounts.AddRangeAsync(items);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteFinancialAccount(int id)
        {
            var entity = await _context.FinancialAccounts.FindAsync(id);
            if (entity != null)
            {
                _context.FinancialAccounts.Remove(entity);
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("Financial account with ID {accountId} not found for deletion", id);
            }
        }

        public async Task<decimal> GetAvailability(int accountId)
        {
            var accountEntity = await _context.FinancialAccounts.FirstOrDefaultAsync(x => x.Id == accountId);            
            return accountEntity?.Availability ?? -1;
        }

        public async Task<FinancialAccount?> GetFinancialAccount(int id)
        {
            var entity = await _context.FinancialAccounts.FindAsync(id);
            return _mapper.Map<FinancialAccount>(entity);
        }

        public async Task<IEnumerable<FinancialAccount>> GetFinancialAccounts()
        {
            var query = _context.FinancialAccounts.AsQueryable();
            var results = await query.ToListAsync();

            return _mapper.Map<IEnumerable<FinancialAccount>>(results);
        }


        public async Task CalculateAvailabilities(IEnumerable<Transaction> transactions)
        {
            var accountIds = transactions.Select(x => x.FinancialAccountId).Distinct();

            foreach(var id in accountIds)
            {
                var sum = transactions.Where(x => x.FinancialAccountId == id).Select(x => x.Amount).Sum();
                await UpdateAvailability(id, sum, false);               
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateAvailability(int accountId, decimal amount, bool saveChanges)
        {
            var accountEntity = await _context.FinancialAccounts.FirstOrDefaultAsync(x => x.Id == accountId);
            if (accountEntity == null)
            {
                _logger.LogWarning("Account with ID {accountId} not found for availability update", accountId);
                return false;
            }
            
            accountEntity.Availability += amount;
            if (saveChanges)
            {
                await _context.SaveChangesAsync();
            }            
            
            return true;
        }

        public async Task UpdateFinancialAccount(FinancialAccount account)
        {
            var modified = _mapper.Map<DataAccess.Entities.FinancialAccount>(account);
            var original = await _context.FinancialAccounts.FindAsync(modified.Id);

            if (original != null)
            {
                _context.Entry(original).CurrentValues.SetValues(modified);
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("Financial account with ID {accountId} not found for update", modified.Id);
            }
        }
    }
}
