using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Exceptions;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Functions.Functions;

/// <summary>
/// Abbinamento dello storico Excel (Spese.xlsx) ai movimenti a DB, solo per utenti admin
/// (verifica in <see cref="ILegacyMatchService"/>). Il file arriva in multipart a ogni richiesta e non viene salvato.
/// </summary>
public class LegacyMatchFunctions : FunctionBase
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private readonly ILegacyMatchService _service;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public LegacyMatchFunctions(ILegacyMatchService service, ICurrentUserAccessor currentUserAccessor)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _currentUserAccessor = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
    }

    [Function("LegacyMatch_Sheets")]
    public async Task<IActionResult> Sheets(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "legacy-match/sheets")] HttpRequest req)
    {
        var (file, _) = await ReadFormAsync(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        await using var stream = file.OpenReadStream();
        return await ExecuteAsync(() => _service.GetSheetsAsync(stream, userId));
    }

    [Function("LegacyMatch_Month")]
    public async Task<IActionResult> Month(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "legacy-match/month")] HttpRequest req)
    {
        var (file, form) = await ReadFormAsync(req);
        var sheet = form["sheet"].ToString();
        if (string.IsNullOrWhiteSpace(sheet))
            throw new BadRequestDomainException("Field 'sheet' is required.");

        var userId = await _currentUserAccessor.GetUserIdAsync();
        await using var stream = file.OpenReadStream();
        return await ExecuteAsync(() => _service.GetMonthAsync(stream, sheet, userId));
    }

    [Function("LegacyMatch_Apply")]
    public async Task<IActionResult> Apply(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "legacy-match/apply")] HttpRequest req)
    {
        var request = await ReadBodyAsync<LegacyMatchApplyRequest>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.ApplyAsync(request, userId));
    }

    [Function("LegacyMatch_Create")]
    public async Task<IActionResult> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "legacy-match/create")] HttpRequest req)
    {
        var request = await ReadBodyAsync<LegacyCreateRequest>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.CreateAsync(request, userId));
    }

    [Function("LegacyMatch_Reset")]
    public async Task<IActionResult> Reset(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "legacy-match/reset")] HttpRequest req)
    {
        var request = await ReadBodyAsync<LegacyResetRequest>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.ResetAsync(request, userId));
    }

    private static async Task<(IFormFile File, IFormCollection Form)> ReadFormAsync(HttpRequest req)
    {
        if (!req.HasFormContentType)
            throw new BadRequestDomainException("Expected a multipart/form-data request with a 'file' field.");

        var form = await req.ReadFormAsync(req.HttpContext.RequestAborted);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
            throw new BadRequestDomainException("No file provided.");
        if (file.Length > MaxFileSizeBytes)
            throw new BadRequestDomainException($"The file exceeds the maximum size of {MaxFileSizeBytes / (1024 * 1024)} MB.");

        return (file, form);
    }
}
