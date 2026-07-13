using FluentResults;

namespace RDS.ExpenseTracker.Domain.Services;

/// <summary>
/// Service for importing transactions from Satispay CSV exports
/// </summary>
public interface ISatisPayCsvImportService
{
    /// <summary>
    /// Import transactions from Satispay CSV file
    /// </summary>
    /// <param name="fileStream">CSV file stream</param>
    /// <param name="fileName">Name of the CSV file</param>
    /// <param name="userId">Id of the user owning the accounts to link transactions to.</param>
    /// <param name="importAll">If false, skip rows with existing ExternalId in database</param>
    /// <returns>Number of transactions imported</returns>
    Task<Result<int>> ImportFromCsvAsync(Stream fileStream, string fileName, int userId, bool importAll = false);
}
