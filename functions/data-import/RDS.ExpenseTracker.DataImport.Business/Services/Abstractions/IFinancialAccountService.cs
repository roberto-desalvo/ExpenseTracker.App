
using RDS.ExpenseTracker.Domain.Models;

namespace RDS.ExpenseTracker.DataImport.Business.Services.Abstractions
{
    public interface IFinancialAccountService
    {
        Task<int> AddFinancialAccount(FinancialAccount account);
        Task<IEnumerable<FinancialAccount>> GetFinancialAccounts();
    }
}
