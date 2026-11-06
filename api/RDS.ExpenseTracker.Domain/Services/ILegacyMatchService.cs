using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;

namespace RDS.ExpenseTracker.Domain.Services;

// Abbinamento delle righe dello storico Excel (Spese.xlsx) ai movimenti a DB. Solo per utenti admin.
public interface ILegacyMatchService : IService
{
    Task<Result<IReadOnlyList<LegacySheetDto>>> GetSheetsAsync(Stream file, int userId);
    Task<Result<LegacyMonthDto>> GetMonthAsync(Stream file, string sheetName, int userId);
    Task<Result<int>> ApplyAsync(LegacyMatchApplyRequest request, int userId);
}
