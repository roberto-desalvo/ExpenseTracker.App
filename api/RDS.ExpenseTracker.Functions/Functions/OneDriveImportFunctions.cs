using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Services;
using RDS.ExpenseTracker.Functions.Auth;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RDS.ExpenseTracker.Functions.Functions;

/// <summary>
/// Import automatico dei file caricati nella cartella OneDrive collegata da ogni utente
/// (<c>/Transazioni/&lt;Banca&gt;/file</c>):
/// <list type="number">
///   <item>Microsoft Graph notifica una modifica nella cartella a <c>OneDrive_Notifications</c>;</item>
///   <item>per ogni utente notificato viene accodato un messaggio e <c>OneDrive_Sync</c> legge la sua delta query,
///         importa i nuovi file e li sposta in <c>Importati</c> / <c>Errori</c>;</item>
///   <item><c>OneDrive_SubscriptionRenewal</c> crea o rinnova ogni giorno le subscription Graph (durano ~29 giorni).</item>
/// </list>
/// Il collegamento è gestito da <see cref="OneDriveConnectionFunctions"/>.
/// Con <c>OneDriveImport:Enabled = false</c> le function non fanno nulla.
/// </summary>
public class OneDriveImportFunctions
{
    public const string SyncQueueName = "onedrive-sync";

    /// <summary>App setting (risolto dall'host) con lo schedule NCRONTAB, in UTC, del rinnovo delle subscription.</summary>
    public const string RenewalScheduleSetting = "OneDriveSubscriptionRenewalSchedule";

    private readonly IOneDriveImportService _oneDriveImportService;
    private readonly IOneDriveImportOptions _options;
    private readonly ILogger<OneDriveImportFunctions> _logger;

    public OneDriveImportFunctions(
        IOneDriveImportService oneDriveImportService,
        IOneDriveImportOptions options,
        ILogger<OneDriveImportFunctions> logger)
    {
        _oneDriveImportService = oneDriveImportService ?? throw new ArgumentNullException(nameof(oneDriveImportService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Endpoint delle change notifications Graph. Anonimo (Graph non invia token): le notifiche sono
    /// autenticate dal <c>clientState</c> del collegamento a cui appartiene la subscription. Risponde subito
    /// (Graph attende al massimo pochi secondi) e delega il lavoro alla coda.
    /// </summary>
    [Function("OneDrive_Notifications")]
    [AllowAnonymousFunction]
    public async Task<OneDriveNotificationOutput> Notifications(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "onedrive/notifications")] HttpRequest req)
    {
        // Validazione alla creazione della subscription: va restituito il token in text/plain.
        if (req.Query.TryGetValue("validationToken", out var validationToken))
        {
            _logger.LogInformation("Graph subscription validation request received");
            return new OneDriveNotificationOutput
            {
                HttpResponse = new ContentResult { Content = validationToken.ToString(), ContentType = "text/plain", StatusCode = 200 },
            };
        }

        if (!_options.Enabled)
            return new OneDriveNotificationOutput { HttpResponse = new AcceptedResult() };

        GraphNotificationBatch? batch;
        try
        {
            batch = await JsonSerializer.DeserializeAsync<GraphNotificationBatch>(req.Body, cancellationToken: req.HttpContext.RequestAborted);
        }
        catch (JsonException)
        {
            return new OneDriveNotificationOutput { HttpResponse = new BadRequestResult() };
        }

        var notifications = (batch?.Value ?? []).Select(n => new OneDriveNotification(n.SubscriptionId, n.ClientState));
        var userIds = await _oneDriveImportService.ResolveNotifiedUsersAsync(notifications, req.HttpContext.RequestAborted);

        if (userIds.Count == 0)
            return new OneDriveNotificationOutput { HttpResponse = new AcceptedResult() };

        _logger.LogInformation("OneDrive change notification received for user(s) {UserIds}: sync queued", string.Join(", ", userIds));

        // Un solo messaggio per utente: la delta query raccoglie comunque tutte le modifiche.
        return new OneDriveNotificationOutput
        {
            HttpResponse = new AcceptedResult(),
            SyncMessages = [.. userIds.Select(SyncMessageFor)],
        };
    }

    /// <summary>
    /// Importa i file caricati dall'utente dopo la sua ultima sincronizzazione. Se un'altra sincronizzazione dello
    /// stesso utente è in corso solleva un'eccezione: il messaggio torna visibile dopo il <c>visibilityTimeout</c>
    /// di host.json e viene ritentato.
    /// </summary>
    [Function("OneDrive_Sync")]
    public async Task Sync([QueueTrigger(SyncQueueName)] OneDriveSyncMessage message, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("OneDrive import disabled: sync message ignored");
            return;
        }

        var result = await _oneDriveImportService.SyncAsync(message.UserId, cancellationToken);
        if (result.IsFailed)
            throw new InvalidOperationException($"OneDrive sync of user {message.UserId} failed: {string.Join("; ", result.Errors.Select(e => e.Message))}");

        _logger.LogInformation("OneDrive sync result for user {UserId}: {@Summary}", message.UserId, result.Value);
    }

    /// <summary>
    /// Per ogni collegamento attivo crea la subscription Graph se manca o la rinnova se scade entro 5 giorni.
    /// </summary>
    [Function("OneDrive_SubscriptionRenewal")]
    public async Task SubscriptionRenewal(
        [TimerTrigger("%" + RenewalScheduleSetting + "%", RunOnStartup = false, UseMonitor = true)] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("OneDrive import disabled: subscription renewal skipped");
            return;
        }

        var summary = await _oneDriveImportService.RenewSubscriptionsAsync(cancellationToken);
        _logger.LogInformation("OneDrive subscription renewal: {@Summary}", summary);
    }

    public static string SyncMessageFor(int userId) => JsonSerializer.Serialize(new OneDriveSyncMessage(userId));

    public sealed record OneDriveSyncMessage(int UserId);

    public sealed class OneDriveNotificationOutput
    {
        [HttpResult]
        public IActionResult HttpResponse { get; set; } = new AcceptedResult();

        [QueueOutput(SyncQueueName)]
        public string[]? SyncMessages { get; set; }
    }

    private sealed record GraphNotificationBatch([property: JsonPropertyName("value")] List<GraphNotification>? Value);

    private sealed record GraphNotification(
        [property: JsonPropertyName("subscriptionId")] string? SubscriptionId,
        [property: JsonPropertyName("clientState")] string? ClientState);
}
