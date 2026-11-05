using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Functions.Functions;

public class AccountTypeFunctions : FunctionBase
{
    private readonly IAccountTypeService _service;

    public AccountTypeFunctions(IAccountTypeService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>Catalogo dei tipi di conto (comune a tutti gli utenti), con l'indicazione di quelli importabili da file.</summary>
    [Function("AccountType_GetAll")]
    public Task<IActionResult> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "account-type")] HttpRequest req)
        => ExecuteAsync(_service.GetAccountTypes);
}
