using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Infrastructure.Repositories;

public class TransferReconciliationRepository : RepositoryBase, ITransferReconciliationRepository
{
    // Coppie salvate per SaveChanges: limita il change tracker e la dimensione delle singole transazioni SQL.
    private const int LinkChunkSize = 500;

    public TransferReconciliationRepository(ExpenseTrackerContext context)
        : base(context)
    {
    }

    public async Task<List<Account>> GetAllAccounts()
    {
        return await Context.Accounts
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Dictionary<int, int>> GetCategoryIdsByKind(CategoryEnum kind)
    {
        // (UserId, Kind) è univoco: al più una categoria per utente
        return await Context.Categories
            .AsNoTracking()
            .Where(c => c.Kind == kind)
            .ToDictionaryAsync(c => c.UserId, c => c.Id);
    }

    public async Task<List<Transaction>> GetUnlinkedTransactions(IReadOnlyCollection<int> accountIds, DateTime? fromDate, DateTime? toDate)
    {
        if (accountIds.Count == 0)
            return [];

        var query = Context.Transactions
            .AsNoTracking()
            .Where(t => accountIds.Contains(t.AccountId) && t.TransferId == null && t.Date != null && t.Amount != 0m);

        if (fromDate.HasValue)
            query = query.Where(t => t.Date >= fromDate);
        if (toDate.HasValue)
            query = query.Where(t => t.Date <= toDate);

        return await query
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Id)
            .Select(t => new Transaction
            {
                Id = t.Id,
                AccountId = t.AccountId,
                Amount = t.Amount,
                Date = t.Date,
                Description = t.Description,
                CategoryId = t.CategoryId,
            })
            .ToListAsync();
    }

    public async Task LinkTransfers(IReadOnlyCollection<(int OutboundTransactionId, int InboundTransactionId)> pairs, int? categoryId)
    {
        var now = DateTime.UtcNow;

        foreach (var chunk in pairs.Chunk(LinkChunkSize))
        {
            foreach (var (outboundId, inboundId) in chunk)
            {
                var transfer = new Transfer { CreatedOn = now };
                Context.Transfers.Add(transfer);

                // stub agganciati senza caricarli: l'UPDATE tocca solo TransferId, CategoryId (se indicata) e UpdatedOn
                foreach (var transactionId in new[] { outboundId, inboundId })
                {
                    var leg = new Transaction { Id = transactionId };
                    Context.Transactions.Attach(leg);
                    leg.TransferNavigation = transfer;
                    if (categoryId.HasValue)
                        leg.CategoryId = categoryId;
                    leg.UpdatedOn = now;
                }
            }

            // un'unica SaveChanges per blocco: insert dei Transfer e update dei movimenti nella stessa transazione
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
        }
    }
}
