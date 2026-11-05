using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Domain.Repositories;

public interface IImportLogRepository : IRepositoryBase
{
    // Salva subito il log, scartando eventuali modifiche non salvate lasciate nel contesto da un import fallito.
    Task SaveImportLog(ImportLog importLog);

    // Aggiorna un log esistente (stato di un import asincrono), con la stessa pulizia del contesto di SaveImportLog.
    Task UpdateImportLog(ImportLog importLog);

    Task<ImportLog?> GetImportLog(int id);
    Task<(IEnumerable<ImportLog> Items, int TotalCount)> GetPagedImportLogs(ImportLogQueryRequest request, int userId);
}
