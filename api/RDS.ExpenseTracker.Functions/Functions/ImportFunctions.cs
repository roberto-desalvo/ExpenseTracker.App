using ExcelDataReader;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Services;
using RDS.ExpenseTracker.Functions.Auth;
using System.Text;

namespace RDS.ExpenseTracker.Functions.Functions;

/// <summary>
/// File import endpoints. Every function requires the <c>Files.Sender</c> app role.
/// Rows already known (by fingerprint / external id) are skipped unless <c>importAll=true</c>.
/// </summary>
[RequireRole("Files.Sender")]
public class ImportFunctions : FunctionBase
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IExcelImportService _excelImportService;
    private readonly IBbvaCsvImportService _bbvaCsvImportService;
    private readonly ITradeRepublicCsvImportService _tradeRepublicCsvImportService;
    private readonly ISatisPayCsvImportService _satisPayCsvImportService;
    private readonly ISellaCsvImportService _sellaCsvImportService;
    private readonly ITestSeedService _testSeedService;
    private readonly ITransferReconciliationService _transferReconciliationService;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ILogger<ImportFunctions> _logger;

    public ImportFunctions(
        IExcelImportService excelImportService,
        IBbvaCsvImportService bbvaCsvImportService,
        ITradeRepublicCsvImportService tradeRepublicCsvImportService,
        ISatisPayCsvImportService satisPayCsvImportService,
        ISellaCsvImportService sellaCsvImportService,
        ITestSeedService testSeedService,
        ITransferReconciliationService transferReconciliationService,
        ICurrentUserAccessor currentUserAccessor,
        ILogger<ImportFunctions> logger)
    {
        _excelImportService              = excelImportService              ?? throw new ArgumentNullException(nameof(excelImportService));
        _bbvaCsvImportService            = bbvaCsvImportService            ?? throw new ArgumentNullException(nameof(bbvaCsvImportService));
        _tradeRepublicCsvImportService   = tradeRepublicCsvImportService   ?? throw new ArgumentNullException(nameof(tradeRepublicCsvImportService));
        _satisPayCsvImportService        = satisPayCsvImportService        ?? throw new ArgumentNullException(nameof(satisPayCsvImportService));
        _sellaCsvImportService           = sellaCsvImportService           ?? throw new ArgumentNullException(nameof(sellaCsvImportService));
        _testSeedService                 = testSeedService                 ?? throw new ArgumentNullException(nameof(testSeedService));
        _transferReconciliationService   = transferReconciliationService   ?? throw new ArgumentNullException(nameof(transferReconciliationService));
        _currentUserAccessor             = currentUserAccessor             ?? throw new ArgumentNullException(nameof(currentUserAccessor));
        _logger                          = logger                          ?? throw new ArgumentNullException(nameof(logger));
    }

    // -------------------------------------------------------------------------
    // Multipart/form-data variants (form field "file")
    // -------------------------------------------------------------------------

    [Function("Import_Excel")]
    public Task<IActionResult> ImportExcel(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/excel")] HttpRequest req)
        => ImportFormFileAsync(req, "Excel", requireCsv: false, _excelImportService.ImportFromExcelAsync);

    [Function("Import_BbvaCsv")]
    public Task<IActionResult> ImportBbvaCsv(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/bbva-csv")] HttpRequest req)
        => ImportFormFileAsync(req, "BBVA CSV", requireCsv: true, _bbvaCsvImportService.ImportFromCsvAsync);

    [Function("Import_TradeRepublicCsv")]
    public Task<IActionResult> ImportTradeRepublicCsv(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/traderepublic-csv")] HttpRequest req)
        => ImportFormFileAsync(req, "Trade Republic CSV", requireCsv: true, _tradeRepublicCsvImportService.ImportFromCsvAsync);

    [Function("Import_SatisPayCsv")]
    public Task<IActionResult> ImportSatisPayCsv(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/satispay-csv")] HttpRequest req)
        => ImportFormFileAsync(req, "Satispay CSV", requireCsv: true, _satisPayCsvImportService.ImportFromCsvAsync);

    [Function("Import_SellaCsv")]
    public Task<IActionResult> ImportSellaCsv(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/sella-csv")] HttpRequest req)
        => ImportFormFileAsync(req, "Sella CSV", requireCsv: true, _sellaCsvImportService.ImportFromCsvAsync);

    // -------------------------------------------------------------------------
    // Base64 JSON variant
    // -------------------------------------------------------------------------

    [Function("Import_ExcelBase64")]
    public async Task<IActionResult> ImportExcelBase64(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/excel/base64")] HttpRequest req)
    {
        var importAll = GetQueryBool(req, "importAll", false);
        var request = await ReadBodyAsync<ImportExcelBase64Request>(req);
        if (string.IsNullOrWhiteSpace(request.Base64Content))
        {
            _logger.LogWarning("Import attempt with invalid base64 payload");
            return new BadRequestObjectResult("No base64 content provided");
        }

        var fileName = string.IsNullOrWhiteSpace(request.FileName) ? "import.xlsx" : request.FileName;
        _logger.LogInformation("Excel base64 import received. FileName={FileName}, Base64Length={Base64Length}, ImportAll={ImportAll}",
            fileName, request.Base64Content.Length, importAll);

        var userId = await _currentUserAccessor.GetUserIdForImportAsync();
        var result = await _excelImportService.ImportFromExcelBase64Async(request.Base64Content, fileName, userId, importAll);

        return ToImportResult("Excel base64", result);
    }

    // -------------------------------------------------------------------------
    // Octet-stream variants (raw body, file name in query string)
    // -------------------------------------------------------------------------

    [Function("Import_ExcelStream")]
    public Task<IActionResult> ImportExcelStream(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/excel/stream")] HttpRequest req)
        => ImportRawBodyAsync(req, "Excel stream", "import.xlsx", requireCsv: false, _excelImportService.ImportFromExcelAsync);

    [Function("Import_BbvaCsvStream")]
    public Task<IActionResult> ImportBbvaCsvStream(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/bbva-csv/stream")] HttpRequest req)
        => ImportRawBodyAsync(req, "BBVA CSV stream", "bbva.csv", requireCsv: true, _bbvaCsvImportService.ImportFromCsvAsync);

    [Function("Import_TradeRepublicCsvStream")]
    public Task<IActionResult> ImportTradeRepublicCsvStream(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/traderepublic-csv/stream")] HttpRequest req)
        => ImportRawBodyAsync(req, "Trade Republic CSV stream", "traderepublic.csv", requireCsv: true, _tradeRepublicCsvImportService.ImportFromCsvAsync);

    [Function("Import_SatisPayCsvStream")]
    public Task<IActionResult> ImportSatisPayCsvStream(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/satispay-csv/stream")] HttpRequest req)
        => ImportRawBodyAsync(req, "Satispay CSV stream", "satispay.csv", requireCsv: true, _satisPayCsvImportService.ImportFromCsvAsync);

    [Function("Import_SellaCsvStream")]
    public Task<IActionResult> ImportSellaCsvStream(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/sella-csv/stream")] HttpRequest req)
        => ImportRawBodyAsync(req, "Sella CSV stream", "sella.csv", requireCsv: true, _sellaCsvImportService.ImportFromCsvAsync);

    // -------------------------------------------------------------------------
    // XLSX variants (raw XLSX body converted to CSV before import)
    // -------------------------------------------------------------------------

    [Function("Import_BbvaXlsx")]
    public Task<IActionResult> ImportBbvaCsvFromXlsx(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/bbva-xlsx")] HttpRequest req)
        => ImportXlsxAsCsvAsync(req, "BBVA XLSX", "bbva.xlsx", ',', _bbvaCsvImportService.ImportFromCsvAsync);

    [Function("Import_TradeRepublicXlsx")]
    public Task<IActionResult> ImportTradeRepublicCsvFromXlsx(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/traderepublic-xlsx")] HttpRequest req)
        => ImportXlsxAsCsvAsync(req, "Trade Republic XLSX", "traderepublic.xlsx", ',', _tradeRepublicCsvImportService.ImportFromCsvAsync);

    [Function("Import_SatisPayXlsx")]
    public Task<IActionResult> ImportSatisPayCsvFromXlsx(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/satispay-xlsx")] HttpRequest req)
        => ImportXlsxAsCsvAsync(req, "Satispay XLSX", "satispay.xlsx", ';', _satisPayCsvImportService.ImportFromCsvAsync);

    [Function("Import_SellaXlsx")]
    public Task<IActionResult> ImportSellaCsvFromXlsx(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/sella-xlsx")] HttpRequest req)
        => ImportXlsxAsCsvAsync(req, "Sella XLSX", "sella.xlsx", ',', _sellaCsvImportService.ImportFromCsvAsync);

    // -------------------------------------------------------------------------
    // Riconciliazione trasferimenti (nessun file in input)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Cerca tra i movimenti già importati di tutti gli utenti le coppie che rappresentano un trasferimento e le collega.
    /// Query opzionali: <c>fromDate</c>, <c>toDate</c> (date ISO) e <c>dryRun=true</c> per vedere i match senza salvarli.
    /// Non dipende dall'identità del chiamante (come <c>Import_TestSeed</c>), ma richiede comunque il ruolo <c>Files.Sender</c>.
    /// </summary>
    [Function("Import_ReconcileTransfers")]
    public async Task<IActionResult> ReconcileTransfers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/transfers/reconcile")] HttpRequest req)
    {
        var dryRun = GetQueryBool(req, "dryRun", false);
        var fromDate = GetQueryDate(req, "fromDate");
        var toDate = GetQueryDate(req, "toDate");

        _logger.LogInformation("Transfer reconciliation requested. FromDate={FromDate}, ToDate={ToDate}, DryRun={DryRun}",
            fromDate, toDate, dryRun);

        return await ExecuteAsync(() => _transferReconciliationService.ReconcileAsync(fromDate, toDate, dryRun));
    }

    // -------------------------------------------------------------------------
    // Test seed (dati fittizi per l'utente di test, nessun file in input)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Elimina e rigenera conti e movimenti fittizi dell'utente <c>test@dev.local</c> (creandolo se assente).
    /// Non dipende dall'identità del chiamante: pensato per un job schedulato che ripristina i dati di test.
    /// </summary>
    [Function("Import_TestSeed")]
    public Task<IActionResult> ImportTestSeed(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import/test-seed")] HttpRequest req)
    {
        _logger.LogInformation("Test seed import requested");
        return ExecuteAsync(_testSeedService.ResetTestUserDataAsync);
    }

    // -------------------------------------------------------------------------
    // Shared pipeline
    // -------------------------------------------------------------------------

    private delegate Task<Result<int>> ImportDelegate(Stream stream, string fileName, int userId, bool importAll);

    private async Task<IActionResult> ImportFormFileAsync(HttpRequest req, string label, bool requireCsv, ImportDelegate import)
    {
        var importAll = GetQueryBool(req, "importAll", false);

        if (!req.HasFormContentType)
            return new BadRequestObjectResult("Expected a multipart/form-data request with a 'file' field.");

        var form = await req.ReadFormAsync(req.HttpContext.RequestAborted);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("{Label} import attempt with invalid file", label);
            return new BadRequestObjectResult("No file provided");
        }

        if (requireCsv && !IsCsvFileName(file.FileName))
            return new BadRequestObjectResult("Invalid file type. Use a .csv file for this endpoint.");

        _logger.LogInformation("{Label} import received. FileName={FileName}, Size={Size} bytes, ImportAll={ImportAll}",
            label, file.FileName, file.Length, importAll);

        var userId = await _currentUserAccessor.GetUserIdForImportAsync();
        using var stream = file.OpenReadStream();
        var result = await import(stream, file.FileName, userId, importAll);

        return ToImportResult(label, result);
    }

    private async Task<IActionResult> ImportRawBodyAsync(HttpRequest req, string label, string defaultFileName, bool requireCsv, ImportDelegate import)
    {
        var importAll = GetQueryBool(req, "importAll", false);
        var fileName = GetQueryString(req, "fileName", defaultFileName);

        if (req.ContentLength is null or 0)
        {
            _logger.LogWarning("{Label} import attempt with empty body", label);
            return new BadRequestObjectResult("No file content provided");
        }

        if (requireCsv && !IsCsvFileName(fileName))
            return new BadRequestObjectResult("Invalid file type. Use a .csv file for this endpoint.");

        _logger.LogInformation("{Label} import received. FileName={FileName}, ContentLength={ContentLength}, ImportAll={ImportAll}",
            label, fileName, req.ContentLength, importAll);

        var userId = await _currentUserAccessor.GetUserIdForImportAsync();
        using var memoryStream = await ReadBodyToMemoryAsync(req);
        var result = await import(memoryStream, fileName, userId, importAll);

        return ToImportResult(label, result);
    }

    private async Task<IActionResult> ImportXlsxAsCsvAsync(HttpRequest req, string label, string fileName, char delimiter, ImportDelegate import)
    {
        var importAll = GetQueryBool(req, "importAll", false);

        if (!string.IsNullOrEmpty(req.ContentType)
            && !req.ContentType.StartsWith(XlsxContentType, StringComparison.OrdinalIgnoreCase))
        {
            return new UnsupportedMediaTypeResult();
        }

        using var xlsxStream = await ReadBodyToMemoryAsync(req);
        if (xlsxStream.Length == 0)
        {
            _logger.LogWarning("{Label} import attempt with no content provided", label);
            return new BadRequestObjectResult("No file content provided");
        }

        _logger.LogInformation("{Label} import received. ByteCount={ByteCount}, ImportAll={ImportAll}",
            label, xlsxStream.Length, importAll);

        var userId = await _currentUserAccessor.GetUserIdForImportAsync();
        using var csvPayload = PrepareCsvPayload(xlsxStream, fileName, delimiter);
        var result = await import(csvPayload.Stream, csvPayload.FileName, userId, importAll);

        return ToImportResult(label, result);
    }

    private IActionResult ToImportResult(string label, Result<int> result)
    {
        if (result.IsFailed)
        {
            _logger.LogWarning("{Label} import failed: {Errors}",
                label, string.Join(", ", result.Errors.Select(e => e.Message)));
            return new BadRequestObjectResult(new { errors = result.Errors.Select(e => e.Message) });
        }

        _logger.LogInformation("{Label} import succeeded. ImportedCount={ImportedCount}", label, result.Value);
        return new OkObjectResult(new { importedCount = result.Value });
    }

    private static async Task<MemoryStream> ReadBodyToMemoryAsync(HttpRequest req)
    {
        var stream = new MemoryStream();
        await req.Body.CopyToAsync(stream, req.HttpContext.RequestAborted);
        stream.Position = 0;
        return stream;
    }

    private CsvPayload PrepareCsvPayload(Stream sourceStream, string fileName, char delimiter)
    {
        var normalizedFileName = string.IsNullOrWhiteSpace(fileName) ? "import.xlsx" : fileName;
        _logger.LogDebug("Read XLSX source stream for {FileName}: {ByteCount} bytes", normalizedFileName, sourceStream.Length);

        var csvContent = ConvertExcelToCsv(sourceStream, delimiter);

        var lineCount = csvContent.Count(c => c == '\n');
        _logger.LogDebug("Converted XLSX to CSV for {FileName}: {LineCount} lines, delimiter='{Delimiter}'",
            normalizedFileName, lineCount, delimiter);

        if (lineCount > 0)
        {
            var firstLine = csvContent.Split('\n', 2)[0];
            _logger.LogDebug("CSV header row for {FileName}: {HeaderLine}", normalizedFileName, firstLine);
        }

        var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        csvStream.Position = 0;

        var csvFileName = Path.ChangeExtension(normalizedFileName, ".csv") ?? "import.csv";
        _logger.LogInformation("Converted XLSX payload to CSV for import. OriginalFileName={OriginalFileName}, ConvertedFileName={ConvertedFileName}, LineCount={LineCount}",
            normalizedFileName,
            csvFileName,
            lineCount);

        return new CsvPayload(csvStream, csvFileName);
    }

    private static bool IsCsvFileName(string? fileName)
        => !string.IsNullOrWhiteSpace(fileName)
            && Path.GetExtension(fileName).Equals(".csv", StringComparison.OrdinalIgnoreCase);

    private static string ConvertExcelToCsv(Stream excelStream, char delimiter = ',')
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

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
                    {
                        csvBuilder.Append(delimiter);
                    }

                    var rawValue = reader.GetValue(columnIndex);
                    var value = rawValue is DateTime dt
                        ? dt.ToString("dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
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
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"");
        return escaped.IndexOfAny([delimiter, '"', '\r', '\n']) >= 0
            ? $"\"{escaped}\""
            : escaped;
    }

    private sealed record CsvPayload(Stream Stream, string FileName) : IDisposable
    {
        public void Dispose() => Stream.Dispose();
    }
}
