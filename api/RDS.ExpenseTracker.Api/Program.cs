using RDS.ExpenseTracker.Api.Middlewares;
using Scalar.AspNetCore;
using RDS.ExpenseTracker.Api.Configuration;
using RDS.ExpenseTracker.Api.Options;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Infrastructure;
using Microsoft.Extensions.Options;
using RDS.ExpenseTracker.Application;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var feCorsPolicy = "FEPolicy";
var debugCorsPolicy = "DebugPolicy";

builder.Services.AddCors(options =>
{
    var appSection = builder.Configuration.GetSection("App");
    options.AddPolicy(feCorsPolicy,
        b =>
        {
            var feUrl = appSection["FEUrl"] ?? string.Empty;
            b.WithOrigins(feUrl)
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });

    if(builder.Environment.IsDevelopment())
    {
        options.AddPolicy(debugCorsPolicy,
            b =>
            {
                b.WithOrigins(appSection.GetSection("AllowedLocalOrigins").Get<string[]>() ?? [])
                    .AllowAnyMethod()
                   .AllowAnyHeader();
        });
    }
});


builder.Configuration.AddEnvironmentVariables();
builder.Services.AddOptions(builder.Configuration);
builder.Services.Configure<BbvaCsvOptions>(builder.Configuration.GetSection(BbvaCsvOptions.SectionName));
builder.Services.Configure<SatisPayCsvOptions>(builder.Configuration.GetSection(SatisPayCsvOptions.SectionName));
builder.Services.Configure<SellaCsvOptions>(builder.Configuration.GetSection(SellaCsvOptions.SectionName));

builder.Services.AddProblemDetails();

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization();

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors(builder.Environment.IsDevelopment() ? debugCorsPolicy : feCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();


app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages();
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapControllers()
    .RequireAuthorization();

app.Run();

