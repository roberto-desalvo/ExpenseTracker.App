using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Functions.Options;

/// <summary>
/// Storage dei file caricati dall'app per l'import asincrono. Lo storage account è quello dell'host Functions
/// (AzureWebJobsStorage); qui si configura solo il container.
/// </summary>
public class ImportStorageOptions : IAppOptions
{
    public const string SectionName = "ImportStorage";

    string IAppOptions.SectionName => SectionName;

    public string ContainerName { get; set; } = "bank-imports";
}
