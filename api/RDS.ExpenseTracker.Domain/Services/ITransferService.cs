using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;

namespace RDS.ExpenseTracker.Domain.Services;

// Ogni operazione è limitata ai trasferimenti tra conti dell'utente indicato (userId).
public interface ITransferService : IService
{
    Task<Result<IEnumerable<TransferDto>>> GetTransfers(int userId);
    Task<Result<TransferDto?>> GetTransfer(int id, int userId);
    Task<Result<int>> AddTransfer(TransferDto transfer, int userId);
    Task<Result> UpdateTransfer(TransferDto transfer, int userId);
    Task<Result> DeleteTransfer(int id, int userId);

    // Ramo mancante di un trasferimento incompleto: i candidati hanno importo opposto, data nello stesso mese,
    // conto diverso e nessun trasferimento completo; il collegamento porta la data del candidato a quella del movimento di partenza.
    Task<Result<IEnumerable<TransactionDto>>> GetCounterpartCandidates(int transactionId, int userId);
    Task<Result> LinkCounterpart(int transactionId, int counterpartId, int userId);
}
