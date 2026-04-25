using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Infrastructure.EFCore;

namespace RDS.ExpenseTracker.Infrastructure.Repositories;

public class TransferRepository : RepositoryBase, ITransferRepository
{
    public TransferRepository(ExpenseTrackerContext context)
        : base(context)
    {
    }

    public async Task<Transfer?> GetTransfer(int id)
    {
        return await Context.Transfers
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<Transfer>> GetTransfers()
    {
        return await Context.Transfers
            .Include(x => x.Transactions)
            .OrderByDescending(x => x.Id)
            .ToListAsync();
    }

    public async Task AddTransfer(Transfer transfer)
    {
        await Context.Transfers.AddAsync(transfer);
    }

    public async Task UpdateTransfer(Transfer transfer)
    {
        var current = await Context.Transfers.FirstOrDefaultAsync(x => x.Id == transfer.Id);
        if (current != null)
        {
            Context.Entry(current).CurrentValues.SetValues(transfer);
        }
    }

    public async Task DeleteTransfer(int id)
    {
        var entity = await Context.Transfers.FirstOrDefaultAsync(x => x.Id == id);
        if (entity != null)
        {
            Context.Transfers.Remove(entity);
        }
    }
}