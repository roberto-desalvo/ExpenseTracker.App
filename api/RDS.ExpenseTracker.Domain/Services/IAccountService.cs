using FluentResults;
using RDS.ExpenseTracker.Api.Dtos;
using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Domain.Services;

public interface IAccountService : IService
{
    Task<Result<IEnumerable<AccountDto>>> GetAccounts();
    Task<Result<AccountDto?>> GetAccount(int id);
    Task<Result<decimal>> GetAvailability(int accountId);
    Task<Result> AddAccounts(IEnumerable<AccountDto> accounts);
    Task<Result> UpdateAccount(AccountDto account);
    Task<Result> DeleteAccount(int id);
}
