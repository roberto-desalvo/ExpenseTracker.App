using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Infrastructure.Repositories;

public class ImportLogRepository : RepositoryBase, IImportLogRepository
{
    public ImportLogRepository(ExpenseTrackerContext context)
        : base(context)
    {
    }

    public async Task SaveImportLog(ImportLog importLog)
    {
        // Un import fallito può lasciare nel contesto (scoped, condiviso con gli altri repository) entità
        // aggiunte e non salvate: non devono finire nel database insieme al log. Dopo un import riuscito
        // il contesto non ha modifiche pendenti, quindi la pulizia non ha effetti.
        Context.ChangeTracker.Clear();

        await Context.ImportLogs.AddAsync(importLog);
        await Context.SaveChangesAsync();
    }
}
