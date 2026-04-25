using FluentResults;
using RDS.ExpenseTracker.Api.Dtos;

namespace RDS.ExpenseTracker.Domain.Services;

public interface IAccountService
{
    Task<Result<IEnumerable<FinancialAccountDto>>> GetAccounts();
    Task<Result<FinancialAccountDto?>> GetAccount(int id);
    Task<Result<decimal>> GetAvailability(int accountId);
    Task<Result> AddAccounts(IEnumerable<FinancialAccountDto> accounts);
    Task<Result> UpdateAccount(FinancialAccountDto account);
    Task<Result> DeleteAccount(int id);
}
