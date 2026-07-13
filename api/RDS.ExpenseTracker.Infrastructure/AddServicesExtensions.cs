using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Infrastructure.Repositories;
using System.Reflection;

namespace RDS.ExpenseTracker.Infrastructure;

public static class AddServicesExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ExpenseTrackerContext>(optBuilder =>
        {
            optBuilder.UseSqlServer(connectionString, builder =>
            {
                builder.EnableRetryOnFailure();
                builder.CommandTimeout(60);
            });
        });

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransferRepository, TransferRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
