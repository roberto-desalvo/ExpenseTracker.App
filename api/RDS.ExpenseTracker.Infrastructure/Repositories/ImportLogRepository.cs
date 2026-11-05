using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
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

    public async Task UpdateImportLog(ImportLog importLog)
    {
        Context.ChangeTracker.Clear();

        importLog.UserNavigation = null;
        importLog.AccountNavigation = null;
        Context.ImportLogs.Update(importLog);
        await Context.SaveChangesAsync();
    }

    public async Task<ImportLog?> GetImportLog(int id)
        => await Context.ImportLogs.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);

    public async Task<(IEnumerable<ImportLog> Items, int TotalCount)> GetPagedImportLogs(ImportLogQueryRequest request, int userId)
    {
        var query = Context.ImportLogs.AsNoTracking().Where(l => l.UserId == userId);

        if (request.Statuses is { Count: > 0 } statuses)
            query = query.Where(l => statuses.Contains(l.Status));

        var totalCount = await query.CountAsync();
        var items = await query
            .Include(l => l.AccountNavigation)
            .OrderByDescending(l => l.ImportedOn)
            .ThenByDescending(l => l.Id)
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
