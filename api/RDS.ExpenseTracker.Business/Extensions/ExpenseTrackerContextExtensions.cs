using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RDS.ExpenseTracker.Business.Services.Abstractions;
using RDS.ExpenseTracker.Business.Services;
using RDS.ExpenseTracker.DataAccess;
using RDS.ExpenseTracker.DataAccess.Entities;
using RDS.ExpenseTracker.DataAccess.Seeds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RDS.ExpenseTracker.Business.Mappings;
using RDS.ExpenseTracker.DataAccess.Utilities;
using RDS.ExpenseTracker.Business.Options;

namespace RDS.ExpenseTracker.Business.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddBusinessServices(this IServiceCollection services, KeyVaultOptions kvOptions)
        {            
            services.AddDbContext<ExpenseTrackerContext>(optBuilder =>
            {                
                var connectionString = AzureKeyVaultHandler.GetKeyVaultSecret(kvOptions.Uri, kvOptions.ConnectionStringSecretName);
                optBuilder.UseSqlServer(connectionString, sqlServerBuilder => sqlServerBuilder.EnableRetryOnFailure());

            });

            services.AddAutoMapper(x => x.AddProfile<ExpenseTrackerBusinessProfile>());
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<IFinancialAccountService, FinancialAccountService>();
            services.AddScoped<ICategoryService, CategoryService>();

        }
    }
}
