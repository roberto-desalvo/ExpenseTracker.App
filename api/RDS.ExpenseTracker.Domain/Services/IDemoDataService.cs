using FluentResults;
using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Domain.Services;

public interface IDemoDataService : IService
{
    Task<Result> GenerateDemoDataAsync(int userId);
}
