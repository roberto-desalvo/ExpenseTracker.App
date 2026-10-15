using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RDS.ExpenseTracker.Application;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Services;
using RDS.ExpenseTracker.Functions.Auth;
using RDS.ExpenseTracker.Functions.Configuration;
using RDS.ExpenseTracker.Functions.Middlewares;
using RDS.ExpenseTracker.Functions.Options;
using RDS.ExpenseTracker.Infrastructure;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

builder.ConfigureFunctionsWebApplication();

// Order matters: logging -> exception handling -> authentication/authorization -> function
builder.UseMiddleware<RequestLoggingMiddleware>();
builder.UseMiddleware<ExceptionHandlingMiddleware>();
builder.UseMiddleware<AuthenticationMiddleware>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddOptions(builder.Configuration);

// The dev authentication bypass must never be active outside local development.
var devAuthOptions = builder.Configuration.BindFromSectionName<DevAuthOptions>();
var isDevelopment = builder.Environment.IsDevelopment()
    || string.Equals(builder.Configuration["AZURE_FUNCTIONS_ENVIRONMENT"], Environments.Development, StringComparison.OrdinalIgnoreCase);
if (devAuthOptions.Enabled && !isDevelopment)
    throw new InvalidOperationException("DevAuth:Enabled can only be set in the Development environment.");

// var kvOptions = builder.Configuration.BindFromSectionName<KeyVaultOptions>();
// var connectionString = AzureKeyVaultHandler.GetKeyVaultSecret(kvOptions.Uri, kvOptions.ConnectionStringSecretName);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
builder.Services.AddInfrastructureServices(connectionString);
builder.Services.AddApplicationServices();

builder.Services.AddScoped<IExpenseExcelFileOptions>(sp =>
    sp.GetRequiredService<IOptions<ExpenseExcelFileOptions>>().Value);

builder.Services.AddScoped<IBbvaCsvOptions>(sp =>
    sp.GetRequiredService<IOptions<BbvaCsvOptions>>().Value);

builder.Services.AddScoped<ITradeRepublicCsvOptions>(sp =>
    sp.GetRequiredService<IOptions<TradeRepublicCsvOptions>>().Value);

builder.Services.AddScoped<ISatisPayCsvOptions>(sp =>
    sp.GetRequiredService<IOptions<SatisPayCsvOptions>>().Value);

builder.Services.AddScoped<ISellaCsvOptions>(sp =>
    sp.GetRequiredService<IOptions<SellaCsvOptions>>().Value);

builder.Services.AddScoped<ITransferMatchingOptions>(sp =>
    sp.GetRequiredService<IOptions<TransferMatchingOptions>>().Value);

builder.Services.AddSingleton<JwtTokenValidator>();
builder.Services.AddScoped<RequestPrincipalAccessor>();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

builder.Build().Run();
