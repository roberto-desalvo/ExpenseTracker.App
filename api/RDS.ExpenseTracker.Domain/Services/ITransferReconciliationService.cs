using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;

namespace RDS.ExpenseTracker.Domain.Services;

public interface ITransferReconciliationService : IService
{
    // Cerca tra i movimenti già importati e non collegati le coppie che rappresentano un trasferimento
    // tra due conti dell'utente e le collega con un Transfer. Con dryRun restituisce i match senza salvarli.
    Task<Result<TransferReconciliationResultDto>> ReconcileAsync(int userId, DateTime? fromDate, DateTime? toDate, bool dryRun);
}
