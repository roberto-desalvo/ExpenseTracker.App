using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;

namespace RDS.ExpenseTracker.Domain.Services;

public interface IUserService : IService
{
    Task<Result<UserDto>> GetOrCreateUserAsync(string azureOid, string email, string? name);
}
