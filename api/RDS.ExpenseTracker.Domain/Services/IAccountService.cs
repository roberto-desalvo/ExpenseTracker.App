using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;

namespace RDS.ExpenseTracker.Domain.Services;

public interface IAccountService : IService
{
    Task<Result<PagedResult<AccountDto>>> GetAccounts(AccountQueryRequest request, int userId);
    Task<Result<AccountDto?>> GetAccount(int id, int userId);
    Task<Result<decimal>> GetAvailability(int accountId, int userId);
    Task<Result> AddAccounts(IEnumerable<AccountDto> accounts, int userId);
    Task<Result> UpdateAccount(AccountDto account, int userId);
}
