using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Exceptions;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Functions.Functions;

/// <summary>
/// Import asincrono dei file caricati dall'app, per qualsiasi utente autenticato (non demo) con un conto di tipo importabile:
/// <list type="number">
///   <item><c>ImportJob_Upload</c> salva il file nello storage, registra l'import come <c>Queued</c> e lo accoda;</item>
///   <item><c>ImportJob_Process</c> elabora il messaggio con la pipeline di import esistente e aggiorna lo stato in <c>ImportLogs</c>;</item>
///   <item><c>ImportJob_Query</c> restituisce lo storico e gli import in corso dell'utente.</item>
/// </list>
/// </summary>
public class ImportJobFunctions : FunctionBase
{
    public const string QueueName = "bank-file-import";

    private readonly IImportJobService _importJobService;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ILogger<ImportJobFunctions> _logger;

    public ImportJobFunctions(
        IImportJobService importJobService,
        ICurrentUserAccessor currentUserAccessor,
        ILogger<ImportJobFunctions> logger)
    {
        _importJobService = importJobService ?? throw new ArgumentNullException(nameof(importJobService));
        _currentUserAccessor = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Multipart/form-data con i campi <c>file</c> (CSV o XLSX esportato dalla banca) e <c>accountId</c>.
    /// Risponde 202 con l'import registrato; l'esito si legge poi da <c>ImportJob_Query</c>.
    /// </summary>
    [Function("ImportJob_Upload")]
    public async Task<ImportJobUploadOutput> Upload(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "imports")] HttpRequest req)
    {
        if (!req.HasFormContentType)
            throw new BadRequestDomainException("Expected a multipart/form-data request with 'file' and 'accountId' fields.");

        var form = await req.ReadFormAsync(req.HttpContext.RequestAborted);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
            throw new BadRequestDomainException("No file provided.");
        if (file.Length > IImportJobService.MaxFileSizeBytes)
            throw new BadRequestDomainException(
                $"The file exceeds the maximum size of {IImportJobService.MaxFileSizeBytes / (1024 * 1024)} MB.");
        if (!int.TryParse(form["accountId"].ToString(), out var accountId))
            throw new BadRequestDomainException("Field 'accountId' is required.");

        var userId = await _currentUserAccessor.GetUserIdAsync();

        // copia in memoria: lo stream del form non è sempre posizionabile e il file è piccolo
        using var content = new MemoryStream();
        await file.CopyToAsync(content, req.HttpContext.RequestAborted);
        content.Position = 0;

        var importLog = Unwrap(await _importJobService.EnqueueAsync(
            userId, accountId, file.FileName, content, req.HttpContext.RequestAborted));

        return new ImportJobUploadOutput
        {
            HttpResponse = new ObjectResult(importLog) { StatusCode = StatusCodes.Status202Accepted },
            QueueMessage = new ImportJobMessage(importLog.Id),
        };
    }

    [Function("ImportJob_Query")]
    public async Task<IActionResult> Query(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "imports/query")] HttpRequest req)
    {
        var request = await ReadBodyAsync<ImportLogQueryRequest>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _importJobService.GetImportLogs(request, userId));
    }

    /// <summary>
    /// Elabora un import in coda. Gli errori dell'import finiscono nel log (stato <c>Failed</c>) senza retry;
    /// solo un errore nel salvataggio dello stato rilancia, e il messaggio viene ritentato.
    /// </summary>
    [Function("ImportJob_Process")]
    public async Task Process([QueueTrigger(QueueName)] ImportJobMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing queued import {ImportLogId}", message.ImportLogId);
        await _importJobService.ProcessAsync(message.ImportLogId, cancellationToken);
    }

    public sealed record ImportJobMessage(int ImportLogId);

    public sealed class ImportJobUploadOutput
    {
        [HttpResult]
        public IActionResult HttpResponse { get; set; } = new AcceptedResult();

        [QueueOutput(QueueName)]
        public ImportJobMessage? QueueMessage { get; set; }
    }
}
