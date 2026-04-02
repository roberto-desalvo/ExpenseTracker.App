using RDS.ExpenseTracker.Domain.Models;

namespace RDS.ExpenseTracker.Business.Services.Abstractions
{
    public interface IFinancialAccountService
    {
        Task AddFinancialAccounts(IEnumerable<FinancialAccount> accounts);
        Task DeleteFinancialAccount(int id);
        Task UpdateFinancialAccount(FinancialAccount account);
        Task<FinancialAccount?> GetFinancialAccount(int id);
        Task<IEnumerable<FinancialAccount>> GetFinancialAccounts();
        Task<bool> UpdateAvailability(int accountId, decimal amount, bool saveChanges);
        Task<decimal> GetAvailability(int accountId);
        Task CalculateAvailabilities(IEnumerable<Transaction> transactions);
    }
}
