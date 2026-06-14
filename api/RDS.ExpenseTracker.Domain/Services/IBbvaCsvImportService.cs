using FluentResults;
using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Domain.Services;

/// <summary>
/// Service for importing transactions from BBVA CSV exports.
/// </summary>
public interface IBbvaCsvImportService : IService
{
    /// <summary>
    /// Imports transactions from BBVA CSV file.
    /// </summary>
    /// <param name="fileStream">Readable stream of the BBVA CSV export.</param>
    /// <param name="fileName">Original file name (used for logging).</param>
    /// <param name="importAll">
    /// When <c>false</c> (default) rows that produce an already-known fingerprint are skipped.
    /// When <c>true</c> all rows are imported regardless.
    /// </param>
    /// <returns>Number of inserted transaction records.</returns>
    Task<Result<int>> ImportFromCsvAsync(Stream fileStream, string fileName, bool importAll = false);
}