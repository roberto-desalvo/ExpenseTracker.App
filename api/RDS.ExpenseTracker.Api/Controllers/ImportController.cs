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
    private readonly ILogger<ImportController> _logger;

    public ImportController(
        IExcelImportService excelImportService,
        IBbvaCsvImportService bbvaCsvImportService,
        ITradeRepublicCsvImportService tradeRepublicCsvImportService,       
        ISatisPayCsvImportService satisPayCsvImportService,
        ISellaCsvImportService sellaCsvImportService,
        ILogger<ImportController> logger)
    {
        _excelImportService              = excelImportService              ?? throw new ArgumentNullException(nameof(excelImportService));
        _bbvaCsvImportService            = bbvaCsvImportService            ?? throw new ArgumentNullException(nameof(bbvaCsvImportService));
        _tradeRepublicCsvImportService   = tradeRepublicCsvImportService   ?? throw new ArgumentNullException(nameof(tradeRepublicCsvImportService));
        _satisPayCsvImportService        = satisPayCsvImportService        ?? throw new ArgumentNullException(nameof(satisPayCsvImportService));
        _sellaCsvImportService           = sellaCsvImportService           ?? throw new ArgumentNullException(nameof(sellaCsvImportService));
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

        using var stream = file.OpenReadStream();
        var result = await _excelImportService.ImportFromExcelAsync(stream, file.FileName, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Import failed: {errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

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
        var result = await _excelImportService.ImportFromExcelBase64Async(request.Base64Content, fileName, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Base64 import failed: {errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

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

        using var stream = file.OpenReadStream();
        var result = await _bbvaCsvImportService.ImportFromCsvAsync(stream, file.FileName, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("BBVA CSV import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

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

        using var stream = file.OpenReadStream();
        var result = await _tradeRepublicCsvImportService.ImportFromCsvAsync(stream, file.FileName, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Trade Republic CSV import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

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

        using var stream = file.OpenReadStream();
        var result = await _satisPayCsvImportService.ImportFromCsvAsync(stream, file.FileName, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Satispay CSV import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

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

        using var stream = file.OpenReadStream();
        var result = await _sellaCsvImportService.ImportFromCsvAsync(stream, file.FileName, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Sella CSV import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

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

        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var result = await _excelImportService.ImportFromExcelAsync(memoryStream, fileName, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Excel stream import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

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

        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var result = await _bbvaCsvImportService.ImportFromCsvAsync(memoryStream, fileName, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("BBVA CSV stream import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

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

        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var result = await _tradeRepublicCsvImportService.ImportFromCsvAsync(memoryStream, fileName, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Trade Republic CSV stream import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

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

        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var result = await _satisPayCsvImportService.ImportFromCsvAsync(memoryStream, fileName, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Satispay CSV stream import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

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

        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var result = await _sellaCsvImportService.ImportFromCsvAsync(memoryStream, fileName, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Sella CSV stream import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        return Ok(new { importedCount = result.Value });
    }

    private async Task<CsvPayload> PrepareCsvPayloadAsync(Stream sourceStream, string fileName)
    {
        var normalizedFileName = string.IsNullOrWhiteSpace(fileName) ? "import.xlsx" : fileName;

        var buffer = new MemoryStream();
        await sourceStream.CopyToAsync(buffer);
        buffer.Position = 0;

        var csvContent = ConvertExcelToCsv(buffer);
        buffer.Dispose();

        var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        csvStream.Position = 0;

        var csvFileName = Path.ChangeExtension(normalizedFileName, ".csv") ?? "import.csv";
        _logger.LogInformation("Converted XLSX payload to CSV for import. OriginalFileName={OriginalFileName}, ConvertedFileName={ConvertedFileName}",
            normalizedFileName,
            csvFileName);

        return new CsvPayload(csvStream, csvFileName);
    }

    [HttpPost("bbva-xlsx")]
    [Consumes("application/json")]
    public async Task<IActionResult> ImportBbvaCsvFromXlsxBase64([FromBody] ImportXlsxBase64EnvelopeRequest request, [FromQuery] bool importAll = false)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("No base64 content provided");
        }

        using var xlsxStream = BuildExcelStreamFromEnvelope(request);
        using var csvPayload = await PrepareCsvPayloadAsync(xlsxStream, "bbva.xlsx");
        var result = await _bbvaCsvImportService.ImportFromCsvAsync(csvPayload.Stream, csvPayload.FileName, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("BBVA XLSX(base64) import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        return Ok(new { importedCount = result.Value });
    }

    [HttpPost("traderepublic-xlsx")]
    [Consumes("application/json")]
    public async Task<IActionResult> ImportTradeRepublicCsvFromXlsxBase64([FromBody] ImportXlsxBase64EnvelopeRequest request, [FromQuery] bool importAll = false)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("No base64 content provided");
        }

        using var xlsxStream = BuildExcelStreamFromEnvelope(request);
        using var csvPayload = await PrepareCsvPayloadAsync(xlsxStream, "traderepublic.xlsx");
        var result = await _tradeRepublicCsvImportService.ImportFromCsvAsync(csvPayload.Stream, csvPayload.FileName, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Trade Republic XLSX(base64) import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        return Ok(new { importedCount = result.Value });
    }

    [HttpPost("satispay-xlsx")]
    [Consumes("application/json")]
    public async Task<IActionResult> ImportSatisPayCsvFromXlsxBase64([FromBody] ImportXlsxBase64EnvelopeRequest request, [FromQuery] bool importAll = false)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("No base64 content provided");
        }

        using var xlsxStream = BuildExcelStreamFromEnvelope(request);
        using var csvPayload = await PrepareCsvPayloadAsync(xlsxStream, "satispay.xlsx");
        var result = await _satisPayCsvImportService.ImportFromCsvAsync(csvPayload.Stream, csvPayload.FileName, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Satispay XLSX(base64) import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        return Ok(new { importedCount = result.Value });
    }

    [HttpPost("sella-xlsx")]
    [Consumes("application/json")]
    public async Task<IActionResult> ImportSellaCsvFromXlsxBase64([FromBody] ImportXlsxBase64EnvelopeRequest request, [FromQuery] bool importAll = false)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("No base64 content provided");
        }

        using var xlsxStream = BuildExcelStreamFromEnvelope(request);
        using var csvPayload = await PrepareCsvPayloadAsync(xlsxStream, "sella.xlsx");
        var result = await _sellaCsvImportService.ImportFromCsvAsync(csvPayload.Stream, csvPayload.FileName, importAll);

        if (result.IsFailed)
        {
            _logger.LogWarning("Sella XLSX(base64) import failed: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        return Ok(new { importedCount = result.Value });
    }

    private static MemoryStream BuildExcelStreamFromEnvelope(ImportXlsxBase64EnvelopeRequest request)
    {
        var normalizedBase64 = NormalizeBase64Payload(request.Content);
        var fileBytes = Convert.FromBase64String(normalizedBase64);
        var stream = new MemoryStream(fileBytes);
        stream.Position = 0;
        return stream;
    }

    private static string NormalizeBase64Payload(string base64Content)
    {
        var trimmed = base64Content.Trim();
        var dataPrefixIndex = trimmed.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
        if (dataPrefixIndex >= 0)
        {
            return trimmed[(dataPrefixIndex + "base64,".Length)..];
        }

        return trimmed;
    }

    private static bool IsCsvFileName(string? fileName)
        => !string.IsNullOrWhiteSpace(fileName)
            && Path.GetExtension(fileName).Equals(".csv", StringComparison.OrdinalIgnoreCase);

    private static string ConvertExcelToCsv(Stream excelStream)
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
                        csvBuilder.Append(',');
                    }

                    var value = reader.GetValue(columnIndex)?.ToString() ?? string.Empty;
                    csvBuilder.Append(EscapeCsvValue(value));
                }

                csvBuilder.AppendLine();
            }
        }
        while (reader.NextResult());

        return csvBuilder.ToString();
    }

    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"");
        return escaped.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{escaped}\""
            : escaped;
    }

    private sealed record CsvPayload(Stream Stream, string FileName) : IDisposable
    {
        public void Dispose() => Stream.Dispose();
    }
}
