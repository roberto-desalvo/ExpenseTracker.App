using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Functions.Functions;

public class AccountFunctions : FunctionBase
{
    private readonly IAccountService _service;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public AccountFunctions(IAccountService service, ICurrentUserAccessor currentUserAccessor)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _currentUserAccessor = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
    }

    [Function("Account_Query")]
    public async Task<IActionResult> Query(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "account/query")] HttpRequest req)
    {
        var request = await ReadBodyAsync<AccountQueryRequest>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetAccounts(request, userId));
    }

    [Function("Account_Get")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "account/{id:int}")] HttpRequest req,
        int id)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetAccount(id, userId));
    }

    [Function("Account_GetAvailability")]
    public async Task<IActionResult> GetAvailability(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "account/{id:int}/availability")] HttpRequest req,
        int id)
    {
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetAvailability(id, userId));
    }

    [Function("Account_Post")]
    public async Task<IActionResult> Post(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "account")] HttpRequest req)
    {
        var dto = await ReadBodyAsync<IEnumerable<AccountDto>>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.AddAccounts(dto, userId));
    }

    [Function("Account_Put")]
    public async Task<IActionResult> Put(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "account")] HttpRequest req)
    {
        var dto = await ReadBodyAsync<AccountDto>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.UpdateAccount(dto, userId));
    }
}
