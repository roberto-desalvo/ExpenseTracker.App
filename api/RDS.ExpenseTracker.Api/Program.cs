using Serilog;
using RDS.ExpenseTracker.Api.Middlewares;
using Scalar.AspNetCore;
using RDS.ExpenseTracker.Application.Extensions;
using RDS.ExpenseTracker.Api.Configuration;
using RDS.ExpenseTracker.Api.Options;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Infrastructure;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var debugCorsPolicy = "Debug";

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(debugCorsPolicy,
            builder =>
            {
                builder.WithOrigins("http://127.0.0.1:5500", "http://127.0.0.1:5173", "http://localhost:5500", "http://localhost:5173")
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
    });
}

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddOptions(builder.Configuration);

builder.Services.AddProblemDetails();

// var kvOptions = builder.Configuration.BindFromSectionName<KeyVaultOptions>();
// var connectionString = AzureKeyVaultHandler.GetKeyVaultSecret(kvOptions.Uri, kvOptions.ConnectionStringSecretName);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
builder.Services.AddInfrastructureServices(connectionString);
builder.Services.AddApplicationServices();

builder.Services.AddScoped<IExpenseExcelFileOptions>(sp =>
    sp.GetRequiredService<IOptions<ExpenseExcelFileOptions>>().Value);

// if (!builder.Environment.IsDevelopment())
// {
//     builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection(builder.Configuration.GetSectionName<AzureAdOptions>()));
//     builder.Services.AddAuthorization();
// }

builder.Services.AddControllers();
builder.Host.UseSerilog();

var app = builder.Build();

app.UseHttpsRedirection();


// TODO - Add authentication and authorization
if (app.Environment.IsDevelopment())
{
    app.UseCors(debugCorsPolicy);
}
// else
// {
//     app.UseAuthentication();
//     app.UseAuthorization();
// }


app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages();
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapControllers()
    .RequireAuthorization();

app.Run();

