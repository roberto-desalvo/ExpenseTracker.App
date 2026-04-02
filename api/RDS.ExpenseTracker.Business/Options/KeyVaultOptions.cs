namespace RDS.ExpenseTracker.Business.Options
{
    public class KeyVaultOptions
    {
        public string Uri { get; set; } = string.Empty;
        public string ConnectionStringSecretName { get; set; } = string.Empty;
    }
}
