using FluentResults;
using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Domain.Services;

public interface ITradeRepublicCsvImportService : IService
{
    /// <param name="fileStream">Readable stream of the Trade Republic CSV export.</param>
    /// <param name="fileName">Original file name (used for logging).</param>
    /// <param name="importAll">
    ///   When <c>false</c> (default) rows whose <c>transaction_id</c> already exist as
    ///   <see cref="Domain.Entities.Transaction.ExternalId"/> are skipped.
    ///   When <c>true</c> all rows are imported regardless.
    /// </param>
    /// <returns>Number of <see cref="Domain.Entities.Transaction"/> records inserted.</returns>
    Task<Result<int>> ImportFromCsvAsync(Stream fileStream, string fileName, int userId, bool importAll = false);
}
