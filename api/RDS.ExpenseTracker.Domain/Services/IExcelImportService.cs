using FluentResults;
using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Domain.Services;

public interface IExcelImportService : IService
{
    Task<Result<int>> ImportFromExcelAsync(Stream fileStream, string fileName, bool importAll = false);
}
