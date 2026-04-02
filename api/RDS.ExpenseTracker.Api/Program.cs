using RDS.ExpenseTracker.Api.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Serilog;
using RDS.ExpenseTracker.Api.Middlewares;
using Scalar.AspNetCore;
using RDS.ExpenseTracker.Business.Extensions;
using RDS.ExpenseTracker.Business.Options;
using RDS.ExpenseTracker.Api.Configuration;

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

builder.Services.AddProblemDetails();

var kvOptions = new KeyVaultOptions();
builder.Configuration.GetSection(SectionNames.KeyVault).Bind(kvOptions);
builder.Services.AddBusinessServices(kvOptions);

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection(SectionNames.AzureAd));
    builder.Services.AddAuthorization();
}

builder.Services.AddAutoMapper(x => x.AddProfile<ExpenseTrackerApiProfile>());
builder.Services.AddControllers();
builder.Host.UseSerilog();

var app = builder.Build();

app.Services.AddSeedData();
app.UseHttpsRedirection();


if (app.Environment.IsDevelopment())
{
    app.UseCors(debugCorsPolicy);
}
else
{
    app.UseAuthentication();
    app.UseAuthorization();
}


app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapControllers()
    .RequireAuthorization();

app.Run();

