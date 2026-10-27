namespace RDS.ExpenseTracker.Domain.Dtos.Requests;

// Range di date (estremi inclusi, si considera solo la parte data) dei movimenti da riclassificare.
public class CategorySyncRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
