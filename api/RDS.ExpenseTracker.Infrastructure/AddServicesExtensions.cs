using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;
using RDS.ExpenseTracker.Infrastructure.OneDrive;
using RDS.ExpenseTracker.Infrastructure.Repositories;
using RDS.ExpenseTracker.Infrastructure.Storage;
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
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAccountTypeRepository, AccountTypeRepository>();
        services.AddScoped<ITransferRepository, TransferRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITransferReconciliationRepository, TransferReconciliationRepository>();
        services.AddScoped<IImportLogRepository, ImportLogRepository>();
        services.AddScoped<IOneDriveSyncStateRepository, OneDriveSyncStateRepository>();

        return services;
    }

    /// <summary>
    /// Storage dei file per gli import asincroni: un container blob nello storage dell'host Functions.
    /// Usa la connection string se presente (Azurite in locale), altrimenti l'identità gestita sull'account indicato.
    /// </summary>
    public static IServiceCollection AddImportFileStorage(
        this IServiceCollection services, string? connectionString, string? accountName, string containerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);

        // risolto al primo uso: gli strumenti (es. dotnet ef) avviano l'host senza la configurazione dello storage
        services.AddSingleton<IImportFileStorage>(_ =>
        {
            BlobServiceClient serviceClient;
            if (!string.IsNullOrWhiteSpace(connectionString))
                serviceClient = new BlobServiceClient(connectionString);
            else if (!string.IsNullOrWhiteSpace(accountName))
                serviceClient = new BlobServiceClient(new Uri($"https://{accountName}.blob.core.windows.net"), new DefaultAzureCredential());
            else
                throw new InvalidOperationException(
                    "Import file storage is not configured: set AzureWebJobsStorage or AzureWebJobsStorage__accountName.");

            return new BlobImportFileStorage(serviceClient.GetBlobContainerClient(containerName));
        });
        return services;
    }

    /// <summary>
    /// Client Microsoft Graph per l'import da OneDrive. Richiede <see cref="IOneDriveImportOptions"/> registrato.
    /// </summary>
    public static IServiceCollection AddOneDriveServices(this IServiceCollection services)
    {
        services.AddHttpClient(OneDriveTokenProvider.HttpClientName);
        services.AddSingleton<IOneDriveTokenProvider, OneDriveTokenProvider>();
        services.AddHttpClient<IOneDriveClient, GraphOneDriveClient>(client =>
        {
            client.BaseAddress = new Uri(GraphOneDriveClient.GraphBaseUrl);
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        return services;
    }
}
