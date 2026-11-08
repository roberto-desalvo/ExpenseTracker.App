using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;

namespace RDS.ExpenseTracker.Domain.Services;

public interface IBudgetService : IService
{
    Task<Result<BudgetMonthDto>> GetMonth(DateTime month, int userId);
    Task<Result> SaveMonth(BudgetMonthUpdateRequest request, int userId);
}
