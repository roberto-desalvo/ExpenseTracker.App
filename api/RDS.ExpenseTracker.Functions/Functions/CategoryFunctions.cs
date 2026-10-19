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

    public CategoryFunctions(ICategoryService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [Function("Category_Query")]
    public async Task<IActionResult> Query(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "category/query")] HttpRequest req)
    {
        var request = await ReadBodyAsync<CategoryQueryRequest>(req);
        return await ExecuteAsync(() => _service.GetCategories(request));
    }

    [Function("Category_Get")]
    public Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "category/{id:int}")] HttpRequest req,
        int id)
        => ExecuteAsync(() => _service.GetCategory(id));

    [Function("Category_GetDefault")]
    public Task<IActionResult> GetDefault(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "category/default")] HttpRequest req)
        => ExecuteAsync(() => _service.GetDefaultCategory());

    [Function("Category_Post")]
    public async Task<IActionResult> Post(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "category")] HttpRequest req)
    {
        var dto = await ReadBodyAsync<IEnumerable<CategoryDto>>(req);
        return await ExecuteAsync(() => _service.AddCategories(dto));
    }

    [Function("Category_Put")]
    public async Task<IActionResult> Put(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "category")] HttpRequest req)
    {
        var dto = await ReadBodyAsync<CategoryDto>(req);
        return await ExecuteAsync(() => _service.UpdateCategory(dto));
    }

    [Function("Category_Delete")]
    public Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "category/{id:int}")] HttpRequest req,
        int id)
        => ExecuteAsync(() => _service.DeleteCategory(id));
}
