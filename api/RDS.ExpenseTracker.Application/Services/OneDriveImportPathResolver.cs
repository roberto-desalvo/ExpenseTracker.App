using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Application.Services;

/// <summary>
/// Regole di instradamento dei file OneDrive: la sottocartella (della cartella osservata) indica la banca,
/// l'estensione il formato. Stesse convenzioni della Logic App che questo flusso sostituisce.
/// </summary>
public static class OneDriveImportPathResolver
{
    private static readonly Dictionary<string, BankFileSource> BankFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BBVA"] = BankFileSource.Bbva,
        ["Sella"] = BankFileSource.Sella,
        ["Satispay"] = BankFileSource.SatisPay,
        ["Trade_Republic"] = BankFileSource.TradeRepublic,
        ["Trade Republic"] = BankFileSource.TradeRepublic,
        ["Excel"] = BankFileSource.Excel,
    };

    /// <summary>Banca associata al nome di una sottocartella; null se la cartella non è prevista.</summary>
    public static BankFileSource? ResolveBankFolder(string? folderName)
        => folderName is not null && BankFolders.TryGetValue(folderName.Trim(), out var source) ? source : null;

    /// <summary>
    /// Formato del file per la banca indicata; null se l'estensione non è supportata
    /// (le banche accettano .csv e .xlsx, la cartella Excel solo .xlsx).
    /// </summary>
    public static BankFileFormat? ResolveFormat(BankFileSource source, string? fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();

        return (source, extension) switch
        {
            (BankFileSource.Excel, ".xlsx") => BankFileFormat.Xlsx,
            (BankFileSource.Excel, _) => null,
            (_, ".csv") => BankFileFormat.Csv,
            (_, ".xlsx") => BankFileFormat.Xlsx,
            _ => null,
        };
    }
}
