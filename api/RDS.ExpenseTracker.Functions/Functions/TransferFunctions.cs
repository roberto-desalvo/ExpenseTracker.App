using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Functions.Functions;

public class TransferFunctions : FunctionBase
{
    private readonly ITransferService _service;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public TransferFunctions(ITransferService service, ICurrentUserAccessor currentUserAccessor)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _currentUserAccessor = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
    }

    [Function("Transfer_GetAll")]
    public async Task<IActionResult> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "transfer")] HttpRequest req)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetTransfers(userId));
    }

    [Function("Transfer_Get")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "transfer/{id:int}")] HttpRequest req,
        int id)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetTransfer(id, userId));
    }

    [Function("Transfer_Post")]
    public async Task<IActionResult> Post(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "transfer")] HttpRequest req)
    {
        var dto = await ReadBodyAsync<TransferDto>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.AddTransfer(dto, userId));
    }

    [Function("Transfer_Put")]
    public async Task<IActionResult> Put(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "transfer/{id:int}")] HttpRequest req,
        int id)
    {
        var dto = await ReadBodyAsync<TransferDto>(req);
        dto.Id = id;
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.UpdateTransfer(dto, userId));
    }

    [Function("Transfer_Delete")]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "transfer/{id:int}")] HttpRequest req,
        int id)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.DeleteTransfer(id, userId));
    }
}
