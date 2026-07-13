using Microsoft.Extensions.DependencyInjection;
using RDS.ExpenseTracker.Application.Mappings;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Services;
using System.Reflection;

namespace RDS.ExpenseTracker.Application;

public static class AddServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {

        services.AddAutoMapper(cfg => {
            cfg.AddProfile<ExpenseTrackerProfile>();
        });
        
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<IExcelImportService, ExcelImportService>();
        services.AddScoped<IBbvaCsvImportService, BbvaCsvImportService>();
        services.AddScoped<ITradeRepublicCsvImportService, TradeRepublicCsvImportService>();
        services.AddScoped<ISatisPayCsvImportService, SatisPayCsvImportService>();
        services.AddScoped<ISellaCsvImportService, SellaCsvImportService>();
        services.AddScoped<ITransferMatchingService, TransferMatchingService>();

        return services;
    }
}
