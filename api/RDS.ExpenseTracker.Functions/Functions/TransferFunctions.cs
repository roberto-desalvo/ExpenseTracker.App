using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Functions.Functions;

public class TransferFunctions : FunctionBase
{
    private readonly ITransferService _service;

    public TransferFunctions(ITransferService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [Function("Transfer_GetAll")]
    public Task<IActionResult> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "transfer")] HttpRequest req)
        => ExecuteAsync(() => _service.GetTransfers());

    [Function("Transfer_Get")]
    public Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "transfer/{id:int}")] HttpRequest req,
        int id)
        => ExecuteAsync(() => _service.GetTransfer(id));

    [Function("Transfer_Post")]
    public async Task<IActionResult> Post(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "transfer")] HttpRequest req)
    {
        var dto = await ReadBodyAsync<TransferDto>(req);
        return await ExecuteAsync(() => _service.AddTransfer(dto));
    }

    [Function("Transfer_Put")]
    public async Task<IActionResult> Put(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "transfer/{id:int}")] HttpRequest req,
        int id)
    {
        var dto = await ReadBodyAsync<TransferDto>(req);
        dto.Id = id;
        return await ExecuteAsync(() => _service.UpdateTransfer(dto));
    }

    [Function("Transfer_Delete")]
    public Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "transfer/{id:int}")] HttpRequest req,
        int id)
        => ExecuteAsync(() => _service.DeleteTransfer(id));
}
