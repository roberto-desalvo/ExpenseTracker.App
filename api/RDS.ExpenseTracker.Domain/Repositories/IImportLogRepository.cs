using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Domain.Repositories;

public interface IImportLogRepository : IRepositoryBase
{
    // Salva subito il log, scartando eventuali modifiche non salvate lasciate nel contesto da un import fallito.
    Task SaveImportLog(ImportLog importLog);
}
