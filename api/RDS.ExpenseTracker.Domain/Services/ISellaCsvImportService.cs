using FluentResults;

namespace RDS.ExpenseTracker.Domain.Services;

public interface ISellaCsvImportService
{
    Task<Result<int>> ImportFromCsvAsync(Stream fileStream, string fileName, int userId, bool importAll = false);
}
