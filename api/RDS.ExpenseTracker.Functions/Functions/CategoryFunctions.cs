using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Functions.Functions;

public class CategoryFunctions : FunctionBase
{
    private readonly ICategoryService _service;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public CategoryFunctions(ICategoryService service, ICurrentUserAccessor currentUserAccessor)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _currentUserAccessor = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
    }

    [Function("Category_Query")]
    public async Task<IActionResult> Query(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "category/query")] HttpRequest req)
    {
        var request = await ReadBodyAsync<CategoryQueryRequest>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetCategories(request, userId));
    }

    [Function("Category_Get")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "category/{id:int}")] HttpRequest req,
        int id)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetCategory(id, userId));
    }

    [Function("Category_GetDefault")]
    public async Task<IActionResult> GetDefault(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "category/default")] HttpRequest req)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetDefaultCategory(userId));
    }

    [Function("Category_Post")]
    public async Task<IActionResult> Post(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "category")] HttpRequest req)
    {
        var dto = await ReadBodyAsync<IEnumerable<CategoryDto>>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.AddCategories(dto, userId));
    }

    [Function("Category_Put")]
    public async Task<IActionResult> Put(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "category")] HttpRequest req)
    {
        var dto = await ReadBodyAsync<CategoryDto>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.UpdateCategory(dto, userId));
    }

    [Function("Category_SyncTransactions")]
    public async Task<IActionResult> SyncTransactions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "category/sync")] HttpRequest req)
    {
        var request = await ReadBodyAsync<CategorySyncRequest>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.SyncTransactionCategories(request, userId));
    }

    [Function("Category_Delete")]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "category/{id:int}")] HttpRequest req,
        int id)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.DeleteCategory(id, userId));
    }
}
