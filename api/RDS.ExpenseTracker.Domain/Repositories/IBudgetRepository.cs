using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Domain.Repositories;

// Previsioni di spesa per categoria e mese; le letture sono filtrate per utente tramite la categoria.
public interface IBudgetRepository : IRepositoryBase
{
    Task<IEnumerable<CategoryBudget>> GetBudgets(DateTime monthStart, int userId);

    // Sostituisce le previsioni del mese dell'utente con quelle indicate (le categorie assenti restano senza previsione).
    Task ReplaceMonthBudgets(DateTime monthStart, IEnumerable<CategoryBudget> budgets, int userId);

    // Spesa (valore assoluto delle uscite) per categoria tra monthStart e endDate, trasferimenti esclusi;
    // CategoryId null raccoglie i movimenti senza categoria.
    Task<IEnumerable<(int? CategoryId, decimal Spent)>> GetSpentByCategory(DateTime monthStart, DateTime endDate, int userId);
}
