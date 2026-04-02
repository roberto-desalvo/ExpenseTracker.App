using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataAccess.Utilities
{
    public class AzureKeyVaultHandler
    {
        public static string GetKeyVaultSecret(string uriAddress, string secretName)
        {
            if (string.IsNullOrWhiteSpace(uriAddress) || string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Invalid KeyVault URI or Secret Name");
            }

            var options = new SecretClientOptions()
            {
                Retry =
                    {
                        Delay= TimeSpan.FromSeconds(2),
                        MaxDelay = TimeSpan.FromSeconds(16),
                        MaxRetries = 5,
                        Mode = RetryMode.Exponential
                     }
            };

            var kvUri = new Uri(uriAddress ?? string.Empty);
            var credentials = new DefaultAzureCredential();
            var secretClient = new SecretClient(kvUri, credentials, options);

            var secret = secretClient.GetSecret(secretName);
            return secret?.Value?.Value ?? string.Empty;
        }
    }
}
