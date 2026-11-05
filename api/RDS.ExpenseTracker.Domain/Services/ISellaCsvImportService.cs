using FluentResults;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Models.DataImport;

namespace RDS.ExpenseTracker.Domain.Services;

public interface ISellaCsvImportService
{
    /// <param name="sellaAccount">Sella account of the user the transactions are imported into.</param>
    Task<Result<int>> ImportFromCsvAsync(Stream fileStream, string fileName, Account sellaAccount, bool importAll = false, ImportStatistics? statistics = null);
}
