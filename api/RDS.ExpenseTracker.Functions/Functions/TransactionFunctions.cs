using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Functions.Functions;

public class TransactionFunctions : FunctionBase
{
    private readonly ITransactionService _service;

    public TransactionFunctions(ITransactionService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [Function("Transaction_Query")]
    public async Task<IActionResult> Query(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "transaction/query")] HttpRequest req)
    {
        var request = await ReadBodyAsync<TransactionQueryRequest>(req);
        return await ExecuteAsync(() => _service.GetPagedTransactions(request));
    }

    [Function("Transaction_GetMonthOptions")]
    public Task<IActionResult> GetMonthOptions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "transaction/month-options")] HttpRequest req)
        => ExecuteAsync(() => _service.GetAvailableMonthOptions());

    [Function("Transaction_GetLanding")]
    public Task<IActionResult> GetLanding(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "transaction/landing")] HttpRequest req)
    {
        var excludeTransfers = GetQueryBool(req, "excludeTransfers", true);
        return ExecuteAsync(() => _service.GetLanding(excludeTransfers));
    }

    [Function("Transaction_Get")]
    public Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "transaction/{id:int}")] HttpRequest req,
        int id)
        => ExecuteAsync(() => _service.GetTransaction(id));

    [Function("Transaction_GetLatest")]
    public Task<IActionResult> GetLatest(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "transaction/latest")] HttpRequest req)
        => ExecuteAsync(() => _service.GetLatestTransaction());

    [Function("Transaction_Post")]
    public async Task<IActionResult> Post(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "transaction")] HttpRequest req)
    {
        var dto = await ReadBodyAsync<TransactionDto>(req);
        return await ExecuteAsync(() => _service.AddTransaction(dto));
    }

    [Function("Transaction_Put")]
    public async Task<IActionResult> Put(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "transaction")] HttpRequest req)
    {
        var dto = await ReadBodyAsync<TransactionDto>(req);
        return await ExecuteAsync(() => _service.UpdateTransaction(dto));
    }

    [Function("Transaction_Delete")]
    public Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "transaction/{id:int}")] HttpRequest req,
        int id)
        => ExecuteAsync(() => _service.DeleteTransaction(id));

    [Function("Transaction_GetTimeSeries")]
    public async Task<IActionResult> GetTimeSeries(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "transaction/series")] HttpRequest req)
    {
        var request = await ReadBodyAsync<TimeSeriesRequestDto>(req);
        return await ExecuteAsync(() => _service.GetTimeSeries(request));
    }

    [Function("Transaction_GetStock")]
    public async Task<IActionResult> GetStock(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "transaction/stock")] HttpRequest req)
    {
        var request = await ReadBodyAsync<TimeSeriesRequestDto>(req);
        return await ExecuteAsync(() => _service.GetStock(request));
    }
}
