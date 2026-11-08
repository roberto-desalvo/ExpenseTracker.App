namespace RDS.ExpenseTracker.Domain.Dtos;

// Pianificazione di un mese: previsione per categoria e, per il mese corrente e i passati, la spesa effettiva.
public class BudgetMonthDto
{
    public DateTime Month { get; set; }

    // Mese corrente o futuro: la previsione è modificabile.
    public bool IsEditable { get; set; }

    // Mese corrente o passato: la spesa effettiva è disponibile (per il mese corrente fino a oggi).
    public bool HasActuals { get; set; }

    public decimal TotalPlanned { get; set; }
    public decimal? TotalSpent { get; set; }

    // Spesa in categorie senza previsione o in movimenti senza categoria.
    public decimal? UnplannedSpent { get; set; }

    public List<BudgetLineDto> Lines { get; set; } = [];
}

public class BudgetLineDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Planned { get; set; }
    public decimal? Spent { get; set; }
}
