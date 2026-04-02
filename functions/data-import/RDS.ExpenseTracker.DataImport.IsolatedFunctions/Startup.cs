using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Api.Dtos;
using RDS.ExpenseTracker.DataImport.Business.Helpers;
using RDS.ExpenseTracker.DataImport.Business.Mappings;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Factories;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Factories.Abstractions;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.Abstractions;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.ExcelDataExtraction;
using RDS.ExpenseTracker.DataImport.Business.Services;
using RDS.ExpenseTracker.DataImport.Business.Services.Abstractions;
using RDS.ExpenseTracker.DataImport.DataAccess.Context;
using RDS.ExpenseTracker.DataImport.DataAccess.Context.Abstractions;
using RDS.ExpenseTracker.DataImport.DataAccess.Repositories;
using RDS.ExpenseTracker.DataImport.DataAccess.Repositories.Abstractions;
using RDS.ExpenseTracker.DataImport.DataAccess.Settings;
using Serilog;
using Serilog.Extensions.Logging;

namespace RDS.ExpenseTracker.DataImport.IsolatedFunctions
{
    public static class IServiceCollectionExtensions
    {
        public static void AddFunctionServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ILoggerProvider>(sp => new SerilogLoggerProvider(Log.Logger, dispose: false));


            services.Configure<ExpenseTrackerApiSettings>(configuration.GetSection(nameof(ExpenseTrackerApiSettings)));
            services.Configure<ApiContextSettings>(configuration.GetSection(nameof(ApiContextSettings)));
            services.Configure<ExpenseTrackerExcelOptions>(configuration.GetSection(nameof(ExpenseTrackerExcelOptions)));

            services.AddHttpClient();

            services.AddScoped<IApiContext, ApiContext>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IRepository<FinancialAccountDto>, FinancialAccountRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();

            services.AddScoped<IFinancialAccountService, FinancialAccountService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ITransactionService, TransactionService>();

            services.AddAutoMapper(x => x.AddProfile<ExpenseTrackerBusinessProfile>());
            services.AddLogging();
            services.AddScoped<IPipelineFactory, PipelineFactory>();
        }

    }
}
