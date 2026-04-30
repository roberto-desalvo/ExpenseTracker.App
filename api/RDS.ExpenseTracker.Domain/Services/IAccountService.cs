using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;

namespace RDS.ExpenseTracker.Domain.Services;

public interface IAccountService : IService
{
    Task<Result<PagedResult<AccountDto>>> GetAccounts(AccountQueryRequest request);
    Task<Result<AccountDto?>> GetAccount(int id);
    Task<Result<decimal>> GetAvailability(int accountId);
    Task<Result> AddAccounts(IEnumerable<AccountDto> accounts);
    Task<Result> UpdateAccount(AccountDto account);
}
