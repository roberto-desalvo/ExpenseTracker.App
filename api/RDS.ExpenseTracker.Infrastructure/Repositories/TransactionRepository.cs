using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Infrastructure.Repositories;

public class TransactionRepository : RepositoryBase, ITransactionRepository
{
    public TransactionRepository(ExpenseTrackerContext context)
        : base(context)
    {
    }

    public async Task AddTransactions(IEnumerable<Transaction> transactions)
    {
        await Context.Transactions.AddRangeAsync(transactions);
    }

    public async Task<int> AddTransaction(Transaction transaction)
    {
        return await AddTransaction(transaction, false);
    }

    public async Task<int> AddTransaction(Transaction transaction, bool saveChanges)
    {
        await Context.Transactions.AddAsync(transaction);

        if (saveChanges)
        {
            await SaveChangesAsync();
        }

        return transaction.Id;
    }

    public async Task DeleteTransaction(int id)
    {
        var entity = await Context.Transactions.FirstOrDefaultAsync(x => x.Id == id);
        if (entity != null)
        {
            Context.Transactions.Remove(entity);
        }
    }

    public async Task<Transaction?> GetTransaction(int id)
    {
        return await Context.Transactions.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Transaction> GetLatestTransaction()
    {
        return await Context.Transactions
            .OrderByDescending(x => x.Date)
            .FirstOrDefaultAsync() ?? new Transaction();
    }

    public async Task UpdateTransaction(Transaction modified)
    {
        var current = await Context.Transactions.FirstOrDefaultAsync(x => x.Id == modified.Id);
        if (current != null)
        {
            Context.Entry(current).CurrentValues.SetValues(modified);
        }
    }

    public async Task<IEnumerable<Transaction>> GetTransactions()
    {
        return await Context.Transactions.ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByTransferId(int transferId)
    {
        return await Context.Transactions
            .Where(x => x.TransferId == transferId)
            .ToListAsync();
    }

    public async Task DeleteAllTransactions()
    {
        var transactions = await Context.Transactions.ToListAsync();
        Context.Transactions.RemoveRange(transactions);
    }

    public async Task ResetTransactions(IEnumerable<Transaction> transactions)
    {
        await DeleteAllTransactions();
        await AddTransactions(transactions);
    }
}
