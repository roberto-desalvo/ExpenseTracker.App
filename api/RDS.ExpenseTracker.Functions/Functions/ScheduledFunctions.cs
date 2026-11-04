using FluentResults;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Functions.Functions;

/// <summary>
/// Timer-triggered jobs. Not HTTP: the authentication and exception middlewares skip them,
/// so failures are rethrown and show up as failed invocations in Application Insights.
/// </summary>
public class ScheduledFunctions
{
    /// <summary>App setting (resolved by the Functions host) holding the NCRONTAB schedule, in UTC.</summary>
    public const string DemoResetScheduleSetting = "DemoResetSchedule";

    private readonly ITestSeedService _testSeedService;
    private readonly ITransferReconciliationService _transferReconciliationService;
    private readonly ILogger<ScheduledFunctions> _logger;

    public ScheduledFunctions(
        ITestSeedService testSeedService,
        ITransferReconciliationService transferReconciliationService,
        ILogger<ScheduledFunctions> logger)
    {
        _testSeedService = testSeedService ?? throw new ArgumentNullException(nameof(testSeedService));
        _transferReconciliationService = transferReconciliationService ?? throw new ArgumentNullException(nameof(transferReconciliationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ripristina ogni giorno i dati fittizi dell'utente demo (<c>test@dev.local</c>) e poi riconcilia
    /// i trasferimenti di tutti gli utenti, in sequenza. Sostituisce la Logic App di reset demo;
    /// gli endpoint HTTP <c>import/test-seed</c> e <c>import/transfers/reconcile</c> restano per le esecuzioni manuali.
    /// </summary>
    [Function("Scheduled_DemoDataReset")]
    public async Task DemoDataReset(
        [TimerTrigger("%" + DemoResetScheduleSetting + "%", RunOnStartup = false, UseMonitor = true)] TimerInfo timer)
    {
        _logger.LogInformation("Scheduled demo data reset started (past due: {IsPastDue})", timer.IsPastDue);

        var seed = EnsureSuccess(await _testSeedService.ResetTestUserDataAsync(), "Demo data reset");
        _logger.LogInformation(
            "Demo data reset: user {UserId} ({Email}), {AccountCount} accounts, {TransactionCount} transactions, {TransferCount} transfers",
            seed.UserId, seed.Email, seed.AccountCount, seed.TransactionCount, seed.TransferCount);

        var reconciliation = EnsureSuccess(
            await _transferReconciliationService.ReconcileAsync(fromDate: null, toDate: null, dryRun: false),
            "Transfer reconciliation");
        _logger.LogInformation("Transfer reconciliation completed: {@Result}", reconciliation);

        if (timer.ScheduleStatus is { } status)
            _logger.LogInformation("Next demo data reset at {Next:o}", status.Next);
    }

    private static T EnsureSuccess<T>(Result<T> result, string step)
    {
        if (result.IsFailed)
            throw new InvalidOperationException($"{step} failed: {string.Join("; ", result.Errors.Select(e => e.Message))}");

        return result.Value;
    }
}
