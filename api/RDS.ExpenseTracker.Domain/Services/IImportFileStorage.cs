using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Domain.Services;

/// <summary>
/// Archivio dei file caricati dall'app per l'import asincrono (blob storage): il file viene salvato
/// alla messa in coda e riletto dalla function che lo elabora.
/// </summary>
public interface IImportFileStorage : IService
{
    Task SaveAsync(string name, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Restituisce il contenuto del file in uno stream posizionabile (il parser XLSX lo richiede).</summary>
    Task<Stream> OpenReadAsync(string name, CancellationToken cancellationToken = default);
}
