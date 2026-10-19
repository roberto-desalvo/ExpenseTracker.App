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
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public TransactionFunctions(ITransactionService service, ICurrentUserAccessor currentUserAccessor)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _currentUserAccessor = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
    }

    [Function("Transaction_Query")]
    public async Task<IActionResult> Query(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "transaction/query")] HttpRequest req)
    {
        var request = await ReadBodyAsync<TransactionQueryRequest>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetPagedTransactions(request, userId));
    }

    [Function("Transaction_GetMonthOptions")]
    public async Task<IActionResult> GetMonthOptions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "transaction/month-options")] HttpRequest req)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetAvailableMonthOptions(userId));
    }

    [Function("Transaction_GetLanding")]
    public async Task<IActionResult> GetLanding(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "transaction/landing")] HttpRequest req)
    {
        var excludeTransfers = GetQueryBool(req, "excludeTransfers", true);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetLanding(userId, excludeTransfers));
    }

    [Function("Transaction_Get")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "transaction/{id:int}")] HttpRequest req,
        int id)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetTransaction(id, userId));
    }

    [Function("Transaction_GetLatest")]
    public async Task<IActionResult> GetLatest(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "transaction/latest")] HttpRequest req)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetLatestTransaction(userId));
    }

    [Function("Transaction_Post")]
    public async Task<IActionResult> Post(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "transaction")] HttpRequest req)
    {
        var dto = await ReadBodyAsync<TransactionDto>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.AddTransaction(dto, userId));
    }

    [Function("Transaction_Put")]
    public async Task<IActionResult> Put(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "transaction")] HttpRequest req)
    {
        var dto = await ReadBodyAsync<TransactionDto>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.UpdateTransaction(dto, userId));
    }

    [Function("Transaction_Delete")]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "transaction/{id:int}")] HttpRequest req,
        int id)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.DeleteTransaction(id, userId));
    }

    [Function("Transaction_GetTimeSeries")]
    public async Task<IActionResult> GetTimeSeries(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "transaction/series")] HttpRequest req)
    {
        var request = await ReadBodyAsync<TimeSeriesRequestDto>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetTimeSeries(request, userId));
    }

    [Function("Transaction_GetStock")]
    public async Task<IActionResult> GetStock(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "transaction/stock")] HttpRequest req)
    {
        var request = await ReadBodyAsync<TimeSeriesRequestDto>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetStock(request, userId));
    }
}
