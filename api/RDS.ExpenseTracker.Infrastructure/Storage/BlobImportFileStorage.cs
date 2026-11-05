using Azure.Storage.Blobs;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Infrastructure.Storage;

/// <summary>
/// File degli import asincroni nel container blob dedicato (nello storage account dell'host Functions).
/// In Azure il container è dichiarato in Bicep; in locale (Azurite) viene creato al primo uso.
/// </summary>
public sealed class BlobImportFileStorage : IImportFileStorage
{
    private readonly BlobContainerClient _container;
    private readonly Lazy<Task> _ensureContainer;

    public BlobImportFileStorage(BlobContainerClient container)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _ensureContainer = new Lazy<Task>(() => _container.CreateIfNotExistsAsync(), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public async Task SaveAsync(string name, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(content);

        await _ensureContainer.Value;
        if (content.CanSeek)
            content.Position = 0;

        await _container.GetBlobClient(name).UploadAsync(content, overwrite: false, cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // file piccoli (limite di upload): in memoria, così lo stream è posizionabile per il parser XLSX
        var buffer = new MemoryStream();
        await _container.GetBlobClient(name).DownloadToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }
}
