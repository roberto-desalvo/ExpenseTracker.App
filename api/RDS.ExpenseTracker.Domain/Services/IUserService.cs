using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;

namespace RDS.ExpenseTracker.Domain.Services;

public interface IUserService : IService
{
    Task<Result<UserDto>> GetOrCreateUserAsync(string azureOid, string email, string? name);

    // Usato solo dal flusso di import (chiamato anche da app/managed identity con token
    // app-only, prive di claim email): prova AzureOid, poi il fallback su AppOid, e crea
    // un nuovo utente solo se è disponibile un'email.
    Task<Result<UserDto>> GetOrCreateUserForImportAsync(string oid, string? email, string? name);
}
