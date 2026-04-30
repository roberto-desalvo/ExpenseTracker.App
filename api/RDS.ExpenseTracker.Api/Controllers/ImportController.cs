using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    private readonly IExcelImportService _excelImportService;
    private readonly ILogger<ImportController> _logger;

    public ImportController(IExcelImportService excelImportService, ILogger<ImportController> logger)
    {
        _excelImportService = excelImportService ?? throw new ArgumentNullException(nameof(excelImportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Imports transactions from an uploaded Excel file.
    /// </summary>
    /// <param name="file">Excel file with transaction data</param>
    /// <param name="importAll">If false (default), filters out already-imported transactions from the current month</param>
    /// <returns>Count of imported transactions</returns>
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

    /// <summary>
    /// Imports transactions from a base64-encoded Excel file.
    /// </summary>
    /// <param name="request">Request containing base64 content and file name</param>
    /// <param name="importAll">If false (default), filters out already-imported transactions from the current month</param>
    /// <returns>Count of imported transactions</returns>
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
}
