using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Exceptions;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Functions.Functions;

public class BudgetFunctions : FunctionBase
{
    private readonly IBudgetService _service;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public BudgetFunctions(IBudgetService service, ICurrentUserAccessor currentUserAccessor)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _currentUserAccessor = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
    }

    [Function("Budget_Get")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "budget")] HttpRequest req)
    {
        var month = GetQueryDate(req, "month")
            ?? throw new BadRequestDomainException("Query parameter 'month' is required.");
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetMonth(month, userId));
    }

    [Function("Budget_GetTotals")]
    public async Task<IActionResult> GetTotals(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "budget/totals")] HttpRequest req)
    {
        var startDate = GetQueryDate(req, "startDate")
            ?? throw new BadRequestDomainException("Query parameter 'startDate' is required.");
        var endDate = GetQueryDate(req, "endDate")
            ?? throw new BadRequestDomainException("Query parameter 'endDate' is required.");
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.GetTotals(startDate, endDate, userId));
    }

    [Function("Budget_Put")]
    public async Task<IActionResult> Put(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "budget")] HttpRequest req)
    {
        var request = await ReadBodyAsync<BudgetMonthUpdateRequest>(req);
        var userId = await _currentUserAccessor.GetUserIdAsync();
        return await ExecuteAsync(() => _service.SaveMonth(request, userId));
    }
}
