using ExcelDataReader;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Services;
using System.Text;

namespace RDS.ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Files.Sender")]
public class ImportController : ControllerBase
{
    private readonly IExcelImportService _excelImportService;
    private readonly IBbvaCsvImportService _bbvaCsvImportService;
    private readonly ITradeRepublicCsvImportService _tradeRepublicCsvImportService;
    private readonly ISatisPayCsvImportService _satisPayCsvImportService;
    private readonly ISellaCsvImportService _sellaCsvImportService;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ILogger<ImportController> _logger;

    public ImportController(
        IExcelImportService excelImportService,
        IBbvaCsvImportService bbvaCsvImportService,
        ITradeRepublicCsvImportService tradeRepublicCsvImportService,
        ISatisPayCsvImportService satisPayCsvImportService,
        ISellaCsvImportService sellaCsvImportService,
        ICurrentUserAccessor currentUserAccessor,
        ILogger<ImportController> logger)
    {
        _excelImportService              = excelImportService              ?? throw new ArgumentNullException(nameof(excelImportService));
        _bbvaCsvImportService            = bbvaCsvImportService            ?? throw new ArgumentNullException(nameof(bbvaCsvImportService));
        _tradeRepublicCsvImportService   = tradeRepublicCsvImportService   ?? throw new ArgumentNullException(nameof(tradeRepublicCsvImportService));
        _satisPayCsvImportService        = satisPayCsvImportService        ?? throw new ArgumentNullException(nameof(satisPayCsvImportService));
        _sellaCsvImportService           = sellaCsvImportService           ?? throw new ArgumentNullException(nameof(sellaCsvImportService));
        _currentUserAccessor             = currentUserAccessor             ?? throw new ArgumentNullException(nameof(currentUserAccessor));
        _logger                          = logger                          ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("excel")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportExcel(IFormFile file, [FromQuery] bool importAll = false)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Import attempt with invalid file");
            return BadRequest("No file provided");
        }

        _logger.LogInformation("Excel import received. FileName={FileName}, Size={Size} bytes, ImportAll={ImportAll}",
            file.FileName, file.Length, importAll);

        var userId = await _currentUserAccessor.GetUserIdAsync();
        using var stream = file.OpenReadStream();
        var result = await _excelImportService.ImportFromExcelAsync(stream, file.FileName, userId, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Import failed: {errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        _logger.LogInformation("Excel import succeeded. FileName={FileName}, ImportedCount={ImportedCount}", file.FileName, result.Value);
        return Ok(new { importedCount = result.Value });
    }

    [HttpPost("excel/base64")]
    [Consumes("application/json")]
    public async Task<IActionResult> ImportExcelBase64([FromBody] ImportExcelBase64Request request, [FromQuery] bool importAll = false)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Base64Content))
        {
            _logger.LogWarning("Import attempt with invalid base64 payload");
            return BadRequest("No base64 content provided");
        }

        var fileName = string.IsNullOrWhiteSpace(request.FileName) ? "import.xlsx" : request.FileName;
        _logger.LogInformation("Excel base64 import received. FileName={FileName}, Base64Length={Base64Length}, ImportAll={ImportAll}",
            fileName, request.Base64Content.Length, importAll);

        var userId = await _currentUserAccessor.GetUserIdAsync();
        var result = await _excelImportService.ImportFromExcelBase64Async(request.Base64Content, fileName, userId, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Base64 import failed: {errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        _logger.LogInformation("Excel base64 import succeeded. FileName={FileName}, ImportedCount={ImportedCount}", fileName, result.Value);
        return Ok(new { importedCount = result.Value });
    }

    /// <summary>
    /// Imports transactions from a BBVA CSV export.
    /// Rows with an already known fingerprint are skipped automatically
    /// unless <paramref name="importAll"/> is <c>true</c>.
    /// </summary>
    [HttpPost("bbva-csv")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportBbvaCsv(IFormFile file, [FromQuery] bool importAll = false)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("BBVA CSV import attempt with invalid file");
            return BadRequest("No file provided");
        }

        if (!IsCsvFileName(file.FileName))
        {
            return BadRequest("Invalid file type. Use a .csv file for this endpoint.");
        }

        _logger.LogInformation("BBVA CSV import received. FileName={FileName}, Size={Size} bytes, ImportAll={ImportAll}",
            file.FileName, file.Length, importAll);

        var userId = await _currentUserAccessor.GetUserIdAsync();
        using var stream = file.OpenReadStream();
        var result = await _bbvaCsvImportService.ImportFromCsvAsync(stream, file.FileName, userId, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("BBVA CSV import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        _logger.LogInformation("BBVA CSV import succeeded. ImportedCount={ImportedCount}", result.Value);
        return Ok(new { importedCount = result.Value });
    }

    /// <summary>
    /// Imports transactions from a Trade Republic CSV export.
    /// Rows whose <c>transaction_id</c> already exist in the database are skipped automatically
    /// unless <paramref name="importAll"/> is <c>true</c>.
    /// </summary>
    [HttpPost("traderepublic-csv")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportTradeRepublicCsv(IFormFile file, [FromQuery] bool importAll = false)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Trade Republic CSV import attempt with invalid file");
            return BadRequest("No file provided");
        }

        if (!IsCsvFileName(file.FileName))
        {
            return BadRequest("Invalid file type. Use a .csv file for this endpoint.");
        }

        _logger.LogInformation("Trade Republic CSV import received. FileName={FileName}, Size={Size} bytes, ImportAll={ImportAll}",
            file.FileName, file.Length, importAll);

        var userId = await _currentUserAccessor.GetUserIdAsync();
        using var stream = file.OpenReadStream();
        var result = await _tradeRepublicCsvImportService.ImportFromCsvAsync(stream, file.FileName, userId, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Trade Republic CSV import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        _logger.LogInformation("Trade Republic CSV import succeeded. ImportedCount={ImportedCount}", result.Value);
        return Ok(new { importedCount = result.Value });
    }

    /// <summary>
    /// Imports transactions from a Satispay CSV export.
    /// Rows whose <c>ID</c> already exist in the database are skipped automatically
    /// unless <paramref name="importAll"/> is <c>true</c>.
    /// </summary>
    [HttpPost("satispay-csv")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportSatisPayCsv(IFormFile file, [FromQuery] bool importAll = false)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Satispay CSV import attempt with invalid file");
            return BadRequest("No file provided");
        }

        if (!IsCsvFileName(file.FileName))
        {
            return BadRequest("Invalid file type. Use a .csv file for this endpoint.");
        }

        _logger.LogInformation("Satispay CSV import received. FileName={FileName}, Size={Size} bytes, ImportAll={ImportAll}",
            file.FileName, file.Length, importAll);

        var userId = await _currentUserAccessor.GetUserIdAsync();
        using var stream = file.OpenReadStream();
        var result = await _satisPayCsvImportService.ImportFromCsvAsync(stream, file.FileName, userId, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Satispay CSV import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        _logger.LogInformation("Satispay CSV import succeeded. ImportedCount={ImportedCount}", result.Value);
        return Ok(new { importedCount = result.Value });
    }

    /// <summary>
    /// Imports transactions from a Sella CSV export.
    /// Rows whose <c>CodiceIdentificativo</c> already exist in the database are skipped automatically
    /// unless <paramref name="importAll"/> is <c>true</c>.
    /// </summary>
    [HttpPost("sella-csv")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportSellaCsv(IFormFile file, [FromQuery] bool importAll = false)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Sella CSV import attempt with invalid file");
            return BadRequest("No file provided");
        }

        if (!IsCsvFileName(file.FileName))
        {
            return BadRequest("Invalid file type. Use a .csv file for this endpoint.");
        }

        _logger.LogInformation("Sella CSV import received. FileName={FileName}, Size={Size} bytes, ImportAll={ImportAll}",
            file.FileName, file.Length, importAll);

        var userId = await _currentUserAccessor.GetUserIdAsync();
        using var stream = file.OpenReadStream();
        var result = await _sellaCsvImportService.ImportFromCsvAsync(stream, file.FileName, userId, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Sella CSV import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        _logger.LogInformation("Sella CSV import succeeded. ImportedCount={ImportedCount}", result.Value);
        return Ok(new { importedCount = result.Value });
    }

    // -------------------------------------------------------------------------
    // Octet-stream variants
    // -------------------------------------------------------------------------

    [HttpPost("excel/stream")]
    [Consumes("application/octet-stream")]
    public async Task<IActionResult> ImportExcelStream([FromQuery] string fileName = "import.xlsx", [FromQuery] bool importAll = false)
    {
        if (Request.ContentLength is null or 0)
        {
            _logger.LogWarning("Excel stream import attempt with empty body");
            return BadRequest("No file content provided");
        }

        _logger.LogInformation("Excel stream import received. FileName={FileName}, ContentLength={ContentLength}, ImportAll={ImportAll}",
            fileName, Request.ContentLength, importAll);

        var userId = await _currentUserAccessor.GetUserIdAsync();
        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var result = await _excelImportService.ImportFromExcelAsync(memoryStream, fileName, userId, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Excel stream import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        _logger.LogInformation("Excel stream import succeeded. FileName={FileName}, ImportedCount={ImportedCount}", fileName, result.Value);
        return Ok(new { importedCount = result.Value });
    }

    /// <summary>
    /// Imports transactions from a BBVA CSV export sent as raw octet-stream.
    /// </summary>
    [HttpPost("bbva-csv/stream")]
    [Consumes("application/octet-stream")]
    public async Task<IActionResult> ImportBbvaCsvStream([FromQuery] string fileName = "bbva.csv", [FromQuery] bool importAll = false)
    {
        if (Request.ContentLength is null or 0)
        {
            _logger.LogWarning("BBVA CSV stream import attempt with empty body");
            return BadRequest("No file content provided");
        }

        if (!IsCsvFileName(fileName))
        {
            return BadRequest("Invalid file type. Use a .csv file for this endpoint.");
        }

        _logger.LogInformation("BBVA CSV stream import received. FileName={FileName}, ContentLength={ContentLength}, ImportAll={ImportAll}",
            fileName, Request.ContentLength, importAll);

        var userId = await _currentUserAccessor.GetUserIdAsync();
        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var result = await _bbvaCsvImportService.ImportFromCsvAsync(memoryStream, fileName, userId, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("BBVA CSV stream import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        _logger.LogInformation("BBVA CSV stream import succeeded. ImportedCount={ImportedCount}", result.Value);
        return Ok(new { importedCount = result.Value });
    }

    /// <summary>
    /// Imports transactions from a Trade Republic CSV export sent as raw octet-stream.
    /// </summary>
    [HttpPost("traderepublic-csv/stream")]
    [Consumes("application/octet-stream")]
    public async Task<IActionResult> ImportTradeRepublicCsvStream([FromQuery] string fileName = "traderepublic.csv", [FromQuery] bool importAll = false)
    {
        if (Request.ContentLength is null or 0)
        {
            _logger.LogWarning("Trade Republic CSV stream import attempt with empty body");
            return BadRequest("No file content provided");
        }

        if (!IsCsvFileName(fileName))
        {
            return BadRequest("Invalid file type. Use a .csv file for this endpoint.");
        }

        _logger.LogInformation("Trade Republic CSV stream import received. FileName={FileName}, ContentLength={ContentLength}, ImportAll={ImportAll}",
            fileName, Request.ContentLength, importAll);

        var userId = await _currentUserAccessor.GetUserIdAsync();
        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var result = await _tradeRepublicCsvImportService.ImportFromCsvAsync(memoryStream, fileName, userId, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Trade Republic CSV stream import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        _logger.LogInformation("Trade Republic CSV stream import succeeded. ImportedCount={ImportedCount}", result.Value);
        return Ok(new { importedCount = result.Value });
    }

    /// <summary>
    /// Imports transactions from a Satispay CSV export sent as raw octet-stream.
    /// </summary>
    [HttpPost("satispay-csv/stream")]
    [Consumes("application/octet-stream")]
    public async Task<IActionResult> ImportSatisPayCsvStream([FromQuery] string fileName = "satispay.csv", [FromQuery] bool importAll = false)
    {
        if (Request.ContentLength is null or 0)
        {
            _logger.LogWarning("Satispay CSV stream import attempt with empty body");
            return BadRequest("No file content provided");
        }

        if (!IsCsvFileName(fileName))
        {
            return BadRequest("Invalid file type. Use a .csv file for this endpoint.");
        }

        _logger.LogInformation("Satispay CSV stream import received. FileName={FileName}, ContentLength={ContentLength}, ImportAll={ImportAll}",
            fileName, Request.ContentLength, importAll);

        var userId = await _currentUserAccessor.GetUserIdAsync();
        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var result = await _satisPayCsvImportService.ImportFromCsvAsync(memoryStream, fileName, userId, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Satispay CSV stream import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        _logger.LogInformation("Satispay CSV stream import succeeded. ImportedCount={ImportedCount}", result.Value);
        return Ok(new { importedCount = result.Value });
    }

    /// <summary>
    /// Imports transactions from a Sella CSV export sent as raw octet-stream.
    /// </summary>
    [HttpPost("sella-csv/stream")]
    [Consumes("application/octet-stream")]
    public async Task<IActionResult> ImportSellaCsvStream([FromQuery] string fileName = "sella.csv", [FromQuery] bool importAll = false)
    {
        if (Request.ContentLength is null or 0)
        {
            _logger.LogWarning("Sella CSV stream import attempt with empty body");
            return BadRequest("No file content provided");
        }

        if (!IsCsvFileName(fileName))
        {
            return BadRequest("Invalid file type. Use a .csv file for this endpoint.");
        }

        _logger.LogInformation("Sella CSV stream import received. FileName={FileName}, ContentLength={ContentLength}, ImportAll={ImportAll}",
            fileName, Request.ContentLength, importAll);

        var userId = await _currentUserAccessor.GetUserIdAsync();
        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var result = await _sellaCsvImportService.ImportFromCsvAsync(memoryStream, fileName, userId, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Sella CSV stream import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        _logger.LogInformation("Sella CSV stream import succeeded. ImportedCount={ImportedCount}", result.Value);
        return Ok(new { importedCount = result.Value });
    }

    private async Task<CsvPayload> PrepareCsvPayloadAsync(Stream sourceStream, string fileName, char delimiter = ',')
    {
        var normalizedFileName = string.IsNullOrWhiteSpace(fileName) ? "import.xlsx" : fileName;

        var buffer = new MemoryStream();
        await sourceStream.CopyToAsync(buffer);
        buffer.Position = 0;
        _logger.LogDebug("Read XLSX source stream for {FileName}: {ByteCount} bytes", normalizedFileName, buffer.Length);

        var csvContent = ConvertExcelToCsv(buffer, delimiter);
        buffer.Dispose();

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

    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpPost("bbva-xlsx")]
    [Consumes(XlsxContentType)]
    public async Task<IActionResult> ImportBbvaCsvFromXlsx([FromQuery] bool importAll = false)
    {
        using var xlsxStream = await ReadXlsxBodyAsync();
        if (xlsxStream.Length == 0)
        {
            _logger.LogWarning("BBVA XLSX import attempt with no content provided");
            return BadRequest("No file content provided");
        }

        _logger.LogInformation("BBVA XLSX import received. ByteCount={ByteCount}, ImportAll={ImportAll}",
            xlsxStream.Length, importAll);

        var userId = await _currentUserAccessor.GetUserIdAsync();
        using var csvPayload = await PrepareCsvPayloadAsync(xlsxStream, "bbva.xlsx");
        var result = await _bbvaCsvImportService.ImportFromCsvAsync(csvPayload.Stream, csvPayload.FileName, userId, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("BBVA XLSX import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        _logger.LogInformation("BBVA XLSX import succeeded. ImportedCount={ImportedCount}", result.Value);
        return Ok(new { importedCount = result.Value });
    }

    [HttpPost("traderepublic-xlsx")]
    [Consumes(XlsxContentType)]
    public async Task<IActionResult> ImportTradeRepublicCsvFromXlsx([FromQuery] bool importAll = false)
    {
        using var xlsxStream = await ReadXlsxBodyAsync();
        if (xlsxStream.Length == 0)
        {
            _logger.LogWarning("Trade Republic XLSX import attempt with no content provided");
            return BadRequest("No file content provided");
        }

        _logger.LogInformation("Trade Republic XLSX import received. ByteCount={ByteCount}, ImportAll={ImportAll}",
            xlsxStream.Length, importAll);

        var userId = await _currentUserAccessor.GetUserIdAsync();
        using var csvPayload = await PrepareCsvPayloadAsync(xlsxStream, "traderepublic.xlsx");
        var result = await _tradeRepublicCsvImportService.ImportFromCsvAsync(csvPayload.Stream, csvPayload.FileName, userId, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Trade Republic XLSX import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        _logger.LogInformation("Trade Republic XLSX import succeeded. ImportedCount={ImportedCount}", result.Value);
        return Ok(new { importedCount = result.Value });
    }

    [HttpPost("satispay-xlsx")]
    [Consumes(XlsxContentType)]
    public async Task<IActionResult> ImportSatisPayCsvFromXlsx([FromQuery] bool importAll = false)
    {
        using var xlsxStream = await ReadXlsxBodyAsync();
        if (xlsxStream.Length == 0)
        {
            _logger.LogWarning("Satispay XLSX import attempt with no content provided");
            return BadRequest("No file content provided");
        }

        _logger.LogInformation("Satispay XLSX import received. ByteCount={ByteCount}, ImportAll={ImportAll}",
            xlsxStream.Length, importAll);

        var userId = await _currentUserAccessor.GetUserIdAsync();
        using var csvPayload = await PrepareCsvPayloadAsync(xlsxStream, "satispay.xlsx", ';');
        var result = await _satisPayCsvImportService.ImportFromCsvAsync(csvPayload.Stream, csvPayload.FileName, userId, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Satispay XLSX import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        _logger.LogInformation("Satispay XLSX import succeeded. ImportedCount={ImportedCount}", result.Value);
        return Ok(new { importedCount = result.Value });
    }

    [HttpPost("sella-xlsx")]
    [Consumes(XlsxContentType)]
    public async Task<IActionResult> ImportSellaCsvFromXlsx([FromQuery] bool importAll = false)
    {
        using var xlsxStream = await ReadXlsxBodyAsync();
        if (xlsxStream.Length == 0)
        {
            _logger.LogWarning("Sella XLSX import attempt with no content provided");
            return BadRequest("No file content provided");
        }

        _logger.LogInformation("Sella XLSX import received. ByteCount={ByteCount}, ImportAll={ImportAll}",
            xlsxStream.Length, importAll);

        var userId = await _currentUserAccessor.GetUserIdAsync();
        using var csvPayload = await PrepareCsvPayloadAsync(xlsxStream, "sella.xlsx");
        var result = await _sellaCsvImportService.ImportFromCsvAsync(csvPayload.Stream, csvPayload.FileName, userId, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Sella XLSX import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        _logger.LogInformation("Sella XLSX import succeeded. ImportedCount={ImportedCount}", result.Value);
        return Ok(new { importedCount = result.Value });
    }

    private async Task<MemoryStream> ReadXlsxBodyAsync()
    {
        var stream = new MemoryStream();
        await Request.Body.CopyToAsync(stream);
        stream.Position = 0;
        return stream;
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
