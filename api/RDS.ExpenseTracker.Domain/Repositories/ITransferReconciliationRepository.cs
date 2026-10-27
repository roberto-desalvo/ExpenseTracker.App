using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Domain.Repositories;

/// <summary>
/// Accesso ai dati per la riconciliazione massiva dei trasferimenti: lavora su tutti gli utenti,
/// legge senza tracking e scrive i collegamenti a blocchi.
/// </summary>
public interface ITransferReconciliationRepository : IRepositoryBase
{
    Task<List<Account>> GetAllAccounts();

    // Movimenti datati, con importo diverso da zero e non collegati, dei conti indicati, ordinati per Date, Id.
    // Le entità restituite non sono tracciate e contengono solo i campi utili al matching.
    Task<List<Transaction>> GetUnlinkedTransactions(IReadOnlyCollection<int> accountIds, DateTime? fromDate, DateTime? toDate);

    // Id della categoria di sistema indicata per ogni utente che la possiede (UserId → CategoryId).
    Task<Dictionary<int, int>> GetCategoryIdsByKind(CategoryEnum kind);

    // Crea un Transfer per ogni coppia e vi collega i due movimenti, impostando la categoria indicata
    // (null = categoria dei movimenti invariata).
    Task LinkTransfers(IReadOnlyCollection<(int OutboundTransactionId, int InboundTransactionId)> pairs, int? categoryId);
}
