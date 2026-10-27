using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Models.DataImport;

namespace RDS.ExpenseTracker.Domain.Services;

public interface IExcelImportService : IService
{
    Task<Result<int>> ImportFromExcelAsync(Stream fileStream, string fileName, int userId, bool importAll = false, ImportStatistics? statistics = null);
    Task<Result<int>> ImportFromExcelBase64Async(string base64Content, string fileName, int userId, bool importAll = false, ImportStatistics? statistics = null);
}
