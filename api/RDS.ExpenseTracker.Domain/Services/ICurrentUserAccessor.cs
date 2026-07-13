namespace RDS.ExpenseTracker.Domain.Services;

public interface ICurrentUserAccessor
{
    Task<int> GetUserIdAsync();
}
