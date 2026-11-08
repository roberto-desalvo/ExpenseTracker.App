namespace RDS.ExpenseTracker.Domain.Entities;

// Previsione di spesa di una categoria per un mese. Appartiene all'utente tramite la categoria.
public class CategoryBudget
{
    public int Id { get; set; }
    public int CategoryId { get; set; }

    // Primo giorno del mese a cui si riferisce la previsione.
    public DateTime Month { get; set; }

    // Importo di spesa previsto (positivo).
    public decimal Amount { get; set; }

    public Category? CategoryNavigation { get; set; }
}
