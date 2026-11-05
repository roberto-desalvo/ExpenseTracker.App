using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Domain.Dtos.Requests;

public class ImportLogQueryRequest : PagedQueryRequest
{
    // Se valorizzato, restituisce solo i log in questi stati (es. gli import in corso).
    public IReadOnlyList<ImportStatus>? Statuses { get; set; }
}
