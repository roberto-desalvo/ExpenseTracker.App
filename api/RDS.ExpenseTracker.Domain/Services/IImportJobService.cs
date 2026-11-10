using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;

namespace RDS.ExpenseTracker.Domain.Services;

/// <summary>
/// Import asincrono dei file caricati dall'app: il file viene salvato nello storage, l'import registrato
/// in <c>ImportLogs</c> come <c>Queued</c> e poi elaborato da una function in coda, che ne aggiorna lo stato.
/// </summary>
public interface IImportJobService : IService
{
    /// <summary>Dimensione massima di un file caricato dall'app.</summary>
    const long MaxFileSizeBytes = 10 * 1024 * 1024;

    /// <summary>Estensioni accettate: CSV ed Excel esportati dalle banche.</summary>
    static readonly IReadOnlyList<string> AllowedExtensions = [".csv", ".xlsx"];

    /// <summary>
    /// Valida la richiesta (utente non demo, conto dell'utente di un tipo importabile, file CSV/XLSX entro il limite),
    /// salva il file e registra l'import come <c>Queued</c>. Il chiamante accoda poi l'id restituito.
    /// </summary>
    Task<Result<ImportLogDto>> EnqueueAsync(int userId, int accountId, string fileName, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elabora un import in coda. Non solleva eccezioni per errori dell'import: l'esito finisce nel log
    /// (<c>Completed</c> o <c>Failed</c>). Un import già concluso viene ignorato (consegna duplicata).
    /// </summary>
    Task ProcessAsync(int importLogId, CancellationToken cancellationToken = default);

    /// <summary>Storico degli import dell'utente (non demo), dal più recente.</summary>
    Task<Result<PagedResult<ImportLogDto>>> GetImportLogs(ImportLogQueryRequest request, int userId);
}
