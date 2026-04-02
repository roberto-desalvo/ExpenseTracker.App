using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.DataImport.IsolatedFunctions;
using Serilog;
using Serilog.Extensions.Hosting;


Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

var builder = FunctionsApplication.CreateBuilder(args);

(builder.Configuration)
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddSingleton<ILoggerProvider>(services => new Serilog.Extensions.Logging.SerilogLoggerProvider(Log.Logger, dispose: false));


builder.Services.AddFunctionServices(builder.Configuration);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
