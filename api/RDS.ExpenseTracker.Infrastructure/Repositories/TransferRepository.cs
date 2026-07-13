using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Repositories;

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

    public async Task<Transfer?> GetTransferByExternalId(string externalId)
    {
        return await Context.Transfers
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.ExternalId == externalId);
    }

    public async Task<IEnumerable<Transfer>> GetTransfers()
    {
        return await Context.Transfers
            .Include(x => x.Transactions)
            .OrderByDescending(x => x.Id)
            .ToListAsync();
    }

    public async Task<Transfer?> GetExistingTransfer(int fromAccountId, int toAccountId, decimal amount, DateTime? date)
    {
        if (!date.HasValue)
        {
            // If no date provided, match by account pair and amount only
            return await Context.Transfers
                .Include(x => x.Transactions)
                .FirstOrDefaultAsync(x =>
                    x.Transactions.Any(t =>
                        t.AccountId == fromAccountId &&
                        t.Amount == -Math.Abs(amount))
                    &&
                    x.Transactions.Any(t =>
                        t.AccountId == toAccountId &&
                        t.Amount == Math.Abs(amount)));
        }

        var fromDate = date.Value.Date;
        var toDate = date.Value.Date.AddDays(1);

        return await Context.Transfers
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x =>
                x.Transactions.Any(t =>
                    t.AccountId == fromAccountId &&
                    t.Amount == -Math.Abs(amount) &&
                    t.Date >= fromDate && t.Date < toDate)
                &&
                x.Transactions.Any(t =>
                    t.AccountId == toAccountId &&
                    t.Amount == Math.Abs(amount) &&
                    t.Date >= fromDate && t.Date < toDate));
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

    public async Task DeleteTransfers(IEnumerable<int> transferIds)
    {
        var ids = transferIds.ToList();
        if (ids.Count == 0) return;

        var transfers = await Context.Transfers
            .Where(x => ids.Contains(x.Id))
            .ToListAsync();
        Context.Transfers.RemoveRange(transfers);
    }
}