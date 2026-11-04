using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Models.DataImport;

namespace RDS.ExpenseTracker.Domain.Services;

/// <summary>
/// Punto d'ingresso unico per l'import di un file di movimenti, usato sia dagli endpoint HTTP
/// sia dall'import automatico da OneDrive. Instrada il file al servizio della banca (convertendo
/// gli XLSX delle banche in CSV) e registra sempre l'esito in <c>ImportLogs</c>.
/// </summary>
public interface IBankFileImportService : IService
{
    /// <param name="source">Banca (o file Excel interno) a cui appartiene il file.</param>
    /// <param name="format">Formato del file; ignorato per <see cref="BankFileSource.Excel"/>.</param>
    /// <param name="content">Contenuto del file.</param>
    /// <param name="fileName">Nome originale del file, registrato in <c>ImportLogs</c>.</param>
    /// <param name="userId">Utente proprietario dei dati importati.</param>
    /// <param name="importAll">Se true importa anche le righe già note.</param>
    /// <param name="logSource">Etichetta per <c>ImportLogs</c>; se null viene ricavata da origine e formato (es. "BBVA CSV").</param>
    /// <remarks>Un'eccezione del servizio di import viene registrata in <c>ImportLogs</c> e poi rilanciata.</remarks>
    Task<Result<int>> ImportAsync(
        BankFileSource source,
        BankFileFormat format,
        Stream content,
        string fileName,
        int userId,
        bool importAll = false,
        string? logSource = null);

    /// <summary>
    /// Esegue un import arbitrario registrandone l'esito in <c>ImportLogs</c> (anche se fallisce o solleva un'eccezione,
    /// che viene poi rilanciata).
    /// </summary>
    Task<Result<int>> RunTrackedAsync(string logSource, string? fileName, int userId, Func<ImportStatistics, Task<Result<int>>> import);

    /// <summary>Etichetta di default per <c>ImportLogs</c> (es. "BBVA CSV", "Sella XLSX", "Excel").</summary>
    static string DefaultLogSource(BankFileSource source, BankFileFormat format)
    {
        var name = source switch
        {
            BankFileSource.Bbva => "BBVA",
            BankFileSource.Sella => "Sella",
            BankFileSource.SatisPay => "Satispay",
            BankFileSource.TradeRepublic => "Trade Republic",
            _ => "Excel",
        };

        return source == BankFileSource.Excel ? name : $"{name} {format.ToString().ToUpperInvariant()}";
    }
}
