using FluentResults;

namespace RDS.ExpenseTracker.Domain.Services;

public interface ISellaPdfImportService
{
    Task<Result<int>> ImportFromPdfAsync(Stream fileStream, string fileName, bool importAll = false);
}
