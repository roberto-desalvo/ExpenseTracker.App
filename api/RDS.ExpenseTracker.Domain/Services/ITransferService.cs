using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;

namespace RDS.ExpenseTracker.Domain.Services;

// Ogni operazione è limitata ai trasferimenti tra conti dell'utente indicato (userId).
public interface ITransferService : IService
{
    Task<Result<IEnumerable<TransferDto>>> GetTransfers(int userId);
    Task<Result<TransferDto?>> GetTransfer(int id, int userId);
    Task<Result<int>> AddTransfer(TransferDto transfer, int userId);
    Task<Result> UpdateTransfer(TransferDto transfer, int userId);
    Task<Result> DeleteTransfer(int id, int userId);

    // Ramo mancante di un trasferimento incompleto, o di un movimento "Money transfers" senza trasferimento
    // (al collegamento si usa il trasferimento del candidato o se ne crea uno): i candidati hanno importo opposto, data nello stesso mese (±1 mese),
    // conto diverso e nessun trasferimento completo; il collegamento porta la data del candidato a quella del movimento di partenza.
    Task<Result<IEnumerable<TransactionDto>>> GetCounterpartCandidates(int transactionId, int userId);
    Task<Result> LinkCounterpart(int transactionId, int counterpartId, int userId);
    // Crea il ramo mancante su un altro conto dell'utente (importo opposto, stessa data, categoria Money transfers); restituisce il suo id.
    Task<Result<int>> CreateCounterpart(int transactionId, NewTransferCounterpart counterpart, int userId);
}
