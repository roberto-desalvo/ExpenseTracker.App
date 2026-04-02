using Azure.Core;
using Azure.Identity;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.DataAccess.Context
{
    public static class TokenHelper
    {
        public static async Task<string> GetAccessTokenAsync(string tenantId, string clientId, string clientSecret, string scope)
        {
            if (Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development")
            {
                var authority = $"https://login.microsoftonline.com/{tenantId}";

                var app = ConfidentialClientApplicationBuilder
                    .Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority(new Uri(authority))
                    .Build();

                var result = await app.AcquireTokenForClient(new[] { scope })
                                      .ExecuteAsync();

                return result.AccessToken;
            }

            var credential = new DefaultAzureCredential();
            var token = await credential.GetTokenAsync(
                new TokenRequestContext(new[] { scope })
            );

            return token.Token;
        }
    }
}
