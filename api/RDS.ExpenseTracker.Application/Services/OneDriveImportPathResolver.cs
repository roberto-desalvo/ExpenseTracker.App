using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Application.Services;

/// <summary>
/// Regole di instradamento dei file OneDrive: la sottocartella (della cartella osservata) indica la banca,
/// l'estensione il formato. La cartella <c>Excel</c> (file storico interno) vale solo per gli admin.
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

    /// <summary>Sottocartelle delle banche create nella cartella dell'app al collegamento di OneDrive.</summary>
    public static readonly IReadOnlyList<string> DefaultBankFolders = ["BBVA", "Sella", "Satispay", "Trade_Republic"];

    /// <summary>
    /// Banca associata al nome di una sottocartella; null se la cartella non è prevista
    /// (o è quella del file storico e <paramref name="allowLegacyExcel"/> è false).
    /// </summary>
    public static BankFileSource? ResolveBankFolder(string? folderName, bool allowLegacyExcel)
    {
        if (folderName is null || !BankFolders.TryGetValue(folderName.Trim(), out var source))
            return null;

        return source == BankFileSource.Excel && !allowLegacyExcel ? null : source;
    }

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
