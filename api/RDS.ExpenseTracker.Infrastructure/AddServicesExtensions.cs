using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Infrastructure.Repositories;
using System.Reflection;

namespace RDS.ExpenseTracker.Infrastructure;

public static class AddServicesExtensions
{
    // Il database (tier free/serverless) può essere in pausa: il primo accesso lo riattiva e la connessione
    // va in timeout (SqlError -2), errore che EF Core di default NON considera transitorio.
    // 5 tentativi aggiuntivi con attesa max 10s restano sotto il limite di ~230s delle richieste HTTP su Azure
    // anche con Connection Timeout=30 (6 × 30s + attese).
    private const int MaxRetryCount = 5;
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(10);
    private static readonly int[] AdditionalTransientErrorNumbers = [-2];

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ExpenseTrackerContext>(optBuilder =>
        {
            optBuilder.UseSqlServer(connectionString, builder =>
            {
                builder.EnableRetryOnFailure(MaxRetryCount, MaxRetryDelay, AdditionalTransientErrorNumbers);
                builder.CommandTimeout(60);
            });
        });

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransferRepository, TransferRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITransferReconciliationRepository, TransferReconciliationRepository>();
        services.AddScoped<IImportLogRepository, ImportLogRepository>();

        return services;
    }
}
