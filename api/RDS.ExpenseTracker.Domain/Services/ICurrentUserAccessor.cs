namespace RDS.ExpenseTracker.Domain.Services;

public interface ICurrentUserAccessor
{
    Task<int> GetUserIdAsync();

    // Usato solo da ImportController: tollera l'assenza della claim email (token
    // app-only di managed identity) e fa fallback sull'oid dell'app associata.
    Task<int> GetUserIdForImportAsync();
}
