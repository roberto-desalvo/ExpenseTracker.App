using ExcelDataReader;
using FluentResults;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Models.DataImport;
using RDS.ExpenseTracker.Domain.Services;
using System.Globalization;
using System.Text;

namespace RDS.ExpenseTracker.Application.Services;

public class BankFileImportService : IBankFileImportService
{
    private readonly IExcelImportService _excelImportService;
    private readonly IBbvaCsvImportService _bbvaCsvImportService;
    private readonly ITradeRepublicCsvImportService _tradeRepublicCsvImportService;
    private readonly ISatisPayCsvImportService _satisPayCsvImportService;
    private readonly ISellaCsvImportService _sellaCsvImportService;
    private readonly IImportLogService _importLogService;
    private readonly ILogger<BankFileImportService> _logger;

    public BankFileImportService(
        IExcelImportService excelImportService,
        IBbvaCsvImportService bbvaCsvImportService,
        ITradeRepublicCsvImportService tradeRepublicCsvImportService,
        ISatisPayCsvImportService satisPayCsvImportService,
        ISellaCsvImportService sellaCsvImportService,
        IImportLogService importLogService,
        ILogger<BankFileImportService> logger)
    {
        _excelImportService            = excelImportService            ?? throw new ArgumentNullException(nameof(excelImportService));
        _bbvaCsvImportService          = bbvaCsvImportService          ?? throw new ArgumentNullException(nameof(bbvaCsvImportService));
        _tradeRepublicCsvImportService = tradeRepublicCsvImportService ?? throw new ArgumentNullException(nameof(tradeRepublicCsvImportService));
        _satisPayCsvImportService      = satisPayCsvImportService      ?? throw new ArgumentNullException(nameof(satisPayCsvImportService));
        _sellaCsvImportService         = sellaCsvImportService         ?? throw new ArgumentNullException(nameof(sellaCsvImportService));
        _importLogService              = importLogService              ?? throw new ArgumentNullException(nameof(importLogService));
        _logger                        = logger                        ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result<int>> ImportAsync(
        BankFileSource source,
        BankFileFormat format,
        Stream content,
        string fileName,
        int userId,
        bool importAll = false,
        string? logSource = null)
    {
        ArgumentNullException.ThrowIfNull(content);

        var label = logSource ?? IBankFileImportService.DefaultLogSource(source, format);

        if (source == BankFileSource.Excel)
        {
            return RunTrackedAsync(label, fileName, userId, statistics =>
                _excelImportService.ImportFromExcelAsync(content, fileName, userId, importAll, statistics));
        }

        var importCsv = GetCsvImporter(source);

        if (format == BankFileFormat.Csv)
        {
            return RunTrackedAsync(label, fileName, userId, statistics =>
                importCsv(content, fileName, userId, importAll, statistics));
        }

        return RunTrackedAsync(label, fileName, userId, async statistics =>
        {
            // conversione dentro il flusso tracciato: un XLSX illeggibile viene registrato come import fallito
            var (csvStream, csvFileName) = ConvertToCsvPayload(content, fileName, GetCsvDelimiter(source));
            await using (csvStream)
            {
                return await importCsv(csvStream, csvFileName, userId, importAll, statistics);
            }
        });
    }

    public async Task<Result<int>> RunTrackedAsync(
        string logSource, string? fileName, int userId, Func<ImportStatistics, Task<Result<int>>> import)
    {
        ArgumentNullException.ThrowIfNull(import);

        var statistics = new ImportStatistics();
        Result<int> result;

        try
        {
            result = await import(statistics);
        }
        catch (Exception ex)
        {
            await _importLogService.RecordAsync(logSource, fileName, userId, statistics, [ex.Message]);
            throw;
        }

        await _importLogService.RecordAsync(logSource, fileName, userId, statistics,
            result.IsFailed ? result.Errors.Select(e => e.Message) : null);

        if (result.IsFailed)
            _logger.LogWarning("{Label} import of {FileName} failed: {Errors}",
                logSource, fileName, string.Join(", ", result.Errors.Select(e => e.Message)));
        else
            _logger.LogInformation("{Label} import of {FileName} succeeded. ImportedCount={ImportedCount}",
                logSource, fileName, result.Value);

        return result;
    }

    /// <summary>Delimitatore del CSV prodotto dalla conversione XLSX, quello atteso dal parser della banca.</summary>
    public static char GetCsvDelimiter(BankFileSource source)
        => source == BankFileSource.SatisPay ? ';' : ',';

    private delegate Task<Result<int>> CsvImporter(Stream stream, string fileName, int userId, bool importAll, ImportStatistics? statistics);

    private CsvImporter GetCsvImporter(BankFileSource source) => source switch
    {
        BankFileSource.Bbva => _bbvaCsvImportService.ImportFromCsvAsync,
        BankFileSource.Sella => _sellaCsvImportService.ImportFromCsvAsync,
        BankFileSource.SatisPay => _satisPayCsvImportService.ImportFromCsvAsync,
        BankFileSource.TradeRepublic => _tradeRepublicCsvImportService.ImportFromCsvAsync,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Not a CSV bank source."),
    };

    private (Stream Stream, string FileName) ConvertToCsvPayload(Stream xlsxStream, string fileName, char delimiter)
    {
        var normalizedFileName = string.IsNullOrWhiteSpace(fileName) ? "import.xlsx" : fileName;

        var csvContent = ConvertExcelToCsv(xlsxStream, delimiter);
        var lineCount = csvContent.Count(c => c == '\n');
        if (lineCount > 0)
            _logger.LogDebug("CSV header row for {FileName}: {HeaderLine}", normalizedFileName, csvContent.Split('\n', 2)[0]);

        var csvFileName = Path.ChangeExtension(normalizedFileName, ".csv");
        _logger.LogInformation(
            "Converted XLSX payload to CSV for import. OriginalFileName={OriginalFileName}, ConvertedFileName={ConvertedFileName}, LineCount={LineCount}",
            normalizedFileName, csvFileName, lineCount);

        return (new MemoryStream(Encoding.UTF8.GetBytes(csvContent)), csvFileName);
    }

    /// <summary>
    /// Converte tutti i fogli di una cartella di lavoro in un unico CSV (date in <c>dd/MM/yyyy HH:mm:ss</c>).
    /// </summary>
    public static string ConvertExcelToCsv(Stream excelStream, char delimiter = ',')
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        if (excelStream.CanSeek)
            excelStream.Position = 0;

        using var reader = ExcelReaderFactory.CreateReader(excelStream);
        var csvBuilder = new StringBuilder();

        do
        {
            while (reader.Read())
            {
                for (var columnIndex = 0; columnIndex < reader.FieldCount; columnIndex++)
                {
                    if (columnIndex > 0)
                        csvBuilder.Append(delimiter);

                    var rawValue = reader.GetValue(columnIndex);
                    var value = rawValue is DateTime dt
                        ? dt.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)
                        : rawValue?.ToString() ?? string.Empty;
                    csvBuilder.Append(EscapeCsvValue(value, delimiter));
                }

                csvBuilder.AppendLine();
            }
        }
        while (reader.NextResult());

        return csvBuilder.ToString();
    }

    private static string EscapeCsvValue(string value, char delimiter)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var escaped = value.Replace("\"", "\"\"");
        return escaped.IndexOfAny([delimiter, '"', '\r', '\n']) >= 0
            ? $"\"{escaped}\""
            : escaped;
    }
}
