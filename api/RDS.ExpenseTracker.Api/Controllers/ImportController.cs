using Microsoft.AspNetCore.Mvc;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    private readonly IExcelImportService _excelImportService;
    private readonly ITradeRepublicCsvImportService _tradeRepublicCsvImportService;
    private readonly ISatisPayCsvImportService _satisPayCsvImportService;
    private readonly ILogger<ImportController> _logger;

    public ImportController(
        IExcelImportService excelImportService,
        ITradeRepublicCsvImportService tradeRepublicCsvImportService,
        ISatisPayCsvImportService satisPayCsvImportService,
        ILogger<ImportController> logger)
    {
        _excelImportService              = excelImportService              ?? throw new ArgumentNullException(nameof(excelImportService));
        _tradeRepublicCsvImportService   = tradeRepublicCsvImportService   ?? throw new ArgumentNullException(nameof(tradeRepublicCsvImportService));
        _satisPayCsvImportService        = satisPayCsvImportService        ?? throw new ArgumentNullException(nameof(satisPayCsvImportService));
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
}
