using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;

namespace RDS.ExpenseTracker.Domain.Services;

public interface IUserService : IService
{
    Task<Result<UserDto?>> GetUserAsync(int id);

    Task<Result<UserDto>> GetOrCreateUserAsync(string azureOid, string email, string? name);

    // Usato solo dal login di sviluppo (DevAuth): recupera l'utente esistente per email
    // senza mai crearne uno nuovo.
    Task<Result<UserDto>> GetUserByEmailAsync(string email);

    // Usato solo dal flusso di import (chiamato anche da app/managed identity con token
    // app-only, prive di claim email): prova AzureOid, poi il fallback su AppOid, e crea
    // un nuovo utente solo se è disponibile un'email.
    Task<Result<UserDto>> GetOrCreateUserForImportAsync(string oid, string? email, string? name);

    // Layout della pagina Analisi: null se l'utente non l'ha mai personalizzato.
    Task<Result<AnalyticsLayoutDto?>> GetAnalyticsLayoutAsync(int userId);

    Task<Result<AnalyticsLayoutDto>> SaveAnalyticsLayoutAsync(int userId, AnalyticsLayoutDto layout);

    Task<Result<CurrentUserDto>> GetCurrentUserAsync(int userId);

    // Fallisce con Forbidden se l'utente non è admin.
    Task<Result> EnsureAdminAsync(int userId);
}
