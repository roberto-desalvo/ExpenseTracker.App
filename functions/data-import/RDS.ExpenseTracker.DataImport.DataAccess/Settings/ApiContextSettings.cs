namespace RDS.ExpenseTracker.DataImport.DataAccess.Settings
{
    public class ApiContextSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
    }
}