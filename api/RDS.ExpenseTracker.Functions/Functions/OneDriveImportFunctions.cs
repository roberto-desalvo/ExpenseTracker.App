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
/// Import automatico dei file caricati nella cartella OneDrive (<c>/Transazioni/&lt;Banca&gt;/file</c>):
/// <list type="number">
///   <item>Microsoft Graph notifica una modifica nella cartella a <c>OneDrive_Notifications</c>;</item>
///   <item>la notifica viene accodata e <c>OneDrive_Sync</c> legge la delta query, importa i nuovi file e li
///         sposta in <c>Importati</c> / <c>Errori</c>;</item>
///   <item><c>OneDrive_SubscriptionRenewal</c> crea o rinnova ogni giorno la subscription Graph (dura ~29 giorni).</item>
/// </list>
/// Sostituisce la Logic App di import. Con <c>OneDriveImport:Enabled = false</c> le function non fanno nulla.
/// </summary>
public class OneDriveImportFunctions
{
    public const string SyncQueueName = "onedrive-sync";

    /// <summary>App setting (risolto dall'host) con lo schedule NCRONTAB, in UTC, del rinnovo della subscription.</summary>
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
    /// autenticate dal <c>clientState</c> condiviso. Risponde subito (Graph attende al massimo pochi secondi)
    /// e delega il lavoro alla coda.
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

        var notifications = batch?.Value ?? [];
        var valid = notifications.Where(n => _oneDriveImportService.IsValidClientState(n.ClientState)).ToList();

        if (valid.Count < notifications.Count)
            _logger.LogWarning("Discarded {Count} OneDrive notifications with an invalid clientState", notifications.Count - valid.Count);

        if (valid.Count == 0)
            return new OneDriveNotificationOutput { HttpResponse = new AcceptedResult() };

        _logger.LogInformation("OneDrive change notification received for subscription(s) {SubscriptionIds}: sync queued",
            string.Join(", ", valid.Select(n => n.SubscriptionId).Distinct()));

        // Un solo messaggio per batch: la delta query raccoglie comunque tutte le modifiche.
        return new OneDriveNotificationOutput
        {
            HttpResponse = new AcceptedResult(),
            SyncMessage = JsonSerializer.Serialize(new { receivedOn = DateTimeOffset.UtcNow }),
        };
    }

    /// <summary>
    /// Importa i file caricati dall'ultima sincronizzazione. Se un'altra sincronizzazione è in corso solleva
    /// un'eccezione: il messaggio torna visibile dopo il <c>visibilityTimeout</c> di host.json e viene ritentato.
    /// </summary>
    [Function("OneDrive_Sync")]
    public async Task Sync([QueueTrigger(SyncQueueName)] string message, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("OneDrive import disabled: sync message ignored");
            return;
        }

        var result = await _oneDriveImportService.SyncAsync(cancellationToken);
        if (result.IsFailed)
            throw new InvalidOperationException($"OneDrive sync failed: {string.Join("; ", result.Errors.Select(e => e.Message))}");

        _logger.LogInformation("OneDrive sync result: {@Summary}", result.Value);
    }

    /// <summary>
    /// Crea la subscription Graph se manca (e la baseline della delta query) o la rinnova se scade entro 5 giorni.
    /// Da eseguire a mano dopo <c>connect-onedrive.ps1</c>: <c>POST /admin/functions/OneDrive_SubscriptionRenewal</c>.
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

        var notificationUrl = ResolveNotificationUrl();
        var result = await _oneDriveImportService.EnsureSubscriptionAsync(notificationUrl, cancellationToken);
        if (result.IsFailed)
            throw new InvalidOperationException($"OneDrive subscription renewal failed: {string.Join("; ", result.Errors.Select(e => e.Message))}");

        _logger.LogInformation("OneDrive subscription {Action}: {SubscriptionId} expires at {ExpiresAt:o}",
            result.Value.Action, result.Value.SubscriptionId, result.Value.ExpiresAt);
    }

    private string ResolveNotificationUrl()
    {
        if (!string.IsNullOrWhiteSpace(_options.NotificationUrl))
            return _options.NotificationUrl;

        // Nome host pubblico della Function App, impostato dalla piattaforma.
        var hostName = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
        if (string.IsNullOrWhiteSpace(hostName))
            throw new InvalidOperationException("Set OneDriveImport:NotificationUrl (WEBSITE_HOSTNAME is not available).");

        return $"https://{hostName}/api/onedrive/notifications";
    }

    public sealed class OneDriveNotificationOutput
    {
        [HttpResult]
        public IActionResult HttpResponse { get; set; } = new AcceptedResult();

        [QueueOutput(SyncQueueName)]
        public string? SyncMessage { get; set; }
    }

    private sealed record GraphNotificationBatch([property: JsonPropertyName("value")] List<GraphNotification>? Value);

    private sealed record GraphNotification(
        [property: JsonPropertyName("subscriptionId")] string? SubscriptionId,
        [property: JsonPropertyName("clientState")] string? ClientState);
}
