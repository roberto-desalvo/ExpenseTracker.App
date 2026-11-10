using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Functions.Functions;

/// <summary>
/// Collegamento del OneDrive personale dell'utente (qualsiasi utente autenticato, non demo) per l'import automatico.
/// Il frontend avvia il collegamento, mostra il codice da inserire sulla pagina Microsoft e chiama
/// <c>OneDriveConnection_Complete</c> a intervalli finché l'utente non ha autorizzato l'app.
/// </summary>
public class OneDriveConnectionFunctions : FunctionBase
{
    private readonly IOneDriveConnectionService _service;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public OneDriveConnectionFunctions(IOneDriveConnectionService service, ICurrentUserAccessor currentUserAccessor)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _currentUserAccessor = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
    }

    [Function("OneDriveConnection_Get")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "onedrive/connection")] HttpRequest req)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetAsync(userId, req.HttpContext.RequestAborted));
    }

    [Function("OneDriveConnection_Start")]
    public async Task<IActionResult> Start(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "onedrive/connection")] HttpRequest req)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.StartAsync(userId, req.HttpContext.RequestAborted));
    }

    [Function("OneDriveConnection_Complete")]
    public async Task<IActionResult> Complete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "onedrive/connection/complete")] HttpRequest req)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.CompleteAsync(userId, req.HttpContext.RequestAborted));
    }

    [Function("OneDriveConnection_Delete")]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "onedrive/connection")] HttpRequest req)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.DisconnectAsync(userId, req.HttpContext.RequestAborted));
    }

    /// <summary>Accoda una sincronizzazione della cartella OneDrive dell'utente (es. se le notifiche non sono attive).</summary>
    [Function("OneDriveConnection_Sync")]
    public async Task<OneDriveSyncOutput> Sync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "onedrive/sync")] HttpRequest req)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        await ExecuteAsync(() => _service.EnsureCanSyncAsync(userId, req.HttpContext.RequestAborted));

        return new OneDriveSyncOutput
        {
            HttpResponse = new AcceptedResult(),
            SyncMessage = OneDriveImportFunctions.SyncMessageFor(userId),
        };
    }

    public sealed class OneDriveSyncOutput
    {
        [HttpResult]
        public IActionResult HttpResponse { get; set; } = new AcceptedResult();

        [QueueOutput(OneDriveImportFunctions.SyncQueueName)]
        public string? SyncMessage { get; set; }
    }
}
