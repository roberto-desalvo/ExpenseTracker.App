using AutoMapper;
using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Application.Services;

public class TransferService : ITransferService
{
    private readonly ITransferRepository _repository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public TransferService(
        ITransferRepository repository,
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository,
        IMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<Result<IEnumerable<TransferDto>>> GetTransfers(int userId)
    {
        var transfers = await _repository.GetTransfers(userId);
        return Result.Ok(_mapper.Map<IEnumerable<TransferDto>>(transfers));
    }

    public async Task<Result<TransferDto?>> GetTransfer(int id, int userId)
    {
        if (id <= 0)
            return Result.Fail(DomainErrors.InvalidId("transfer", id));

        var transfer = await _repository.GetTransfer(id, userId);
        if (transfer is null)
            return Result.Fail(DomainErrors.NotFound("Transfer", id));

        return Result.Ok<TransferDto?>(_mapper.Map<TransferDto>(transfer));
    }

    public async Task<Result<int>> AddTransfer(TransferDto dto, int userId)
    {
        var validation = await ValidateTransferRequest(dto, userId);
        if (validation.IsFailed)
            return Result.Fail(validation.Errors);

        var transfer = new Transfer
        {
            CreatedOn = DateTime.UtcNow
        };

        var transferDate = dto.Date ?? DateTime.UtcNow;
        var moneyTransfersId = await _categoryRepository.GetCategoryIdByKind(userId, CategoryEnum.MoneyTransfers);

        var fromTransaction = new Transaction
        {
            AccountId = dto.FromAccountId,
            CategoryId = moneyTransfersId,
            Amount = -Math.Abs(dto.Amount),
            Description = dto.Description,
            Date = transferDate,
            CreatedOn = DateTime.UtcNow,
            TransferNavigation = transfer
        };

        var toTransaction = new Transaction
        {
            AccountId = dto.ToAccountId,
            CategoryId = moneyTransfersId,
            Amount = Math.Abs(dto.Amount),
            Description = dto.Description,
            Date = transferDate,
            CreatedOn = DateTime.UtcNow,
            TransferNavigation = transfer
        };

        await _repository.AddTransfer(transfer);
        await _transactionRepository.AddTransactions([fromTransaction, toTransaction]);
        await _repository.SaveChangesAsync();

        return Result.Ok(transfer.Id);
    }

    public async Task<Result> UpdateTransfer(TransferDto dto, int userId)
    {
        if (dto.Id <= 0)
            return Result.Fail(DomainErrors.InvalidId("transfer", dto.Id));

        var transfer = await _repository.GetTransfer(dto.Id, userId);
        if (transfer is null)
            return Result.Fail(DomainErrors.NotFound("Transfer", dto.Id));

        var validation = await ValidateTransferRequest(dto, userId);
        if (validation.IsFailed)
            return Result.Fail(validation.Errors);

        var existingTransactions = await _transactionRepository.GetTransactionsByTransferId(dto.Id);
        foreach (var transaction in existingTransactions)
        {
            await _transactionRepository.DeleteTransaction(transaction.Id);
        }

        var transferDate = dto.Date ?? DateTime.UtcNow;
        var moneyTransfersId = await _categoryRepository.GetCategoryIdByKind(userId, CategoryEnum.MoneyTransfers);

        await _transactionRepository.AddTransactions([
            new Transaction
            {
                AccountId = dto.FromAccountId,
                CategoryId = moneyTransfersId,
                Amount = -Math.Abs(dto.Amount),
                Description = dto.Description,
                Date = transferDate,
                CreatedOn = DateTime.UtcNow,
                TransferId = dto.Id
            },
            new Transaction
            {
                AccountId = dto.ToAccountId,
                CategoryId = moneyTransfersId,
                Amount = Math.Abs(dto.Amount),
                Description = dto.Description,
                Date = transferDate,
                CreatedOn = DateTime.UtcNow,
                TransferId = dto.Id
            }
        ]);

        await _repository.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> DeleteTransfer(int id, int userId)
    {
        if (id <= 0)
            return Result.Fail(DomainErrors.InvalidId("transfer", id));

        var transfer = await _repository.GetTransfer(id, userId);
        if (transfer is null)
            return Result.Fail(DomainErrors.NotFound("Transfer", id));

        var existingTransactions = await _transactionRepository.GetTransactionsByTransferId(id);
        foreach (var transaction in existingTransactions)
        {
            await _transactionRepository.DeleteTransaction(transaction.Id);
        }

        await _repository.DeleteTransfer(id);
        await _repository.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result<IEnumerable<TransactionDto>>> GetCounterpartCandidates(int transactionId, int userId)
    {
        var source = await LoadIncompleteLeg(transactionId, userId);
        if (source.IsFailed)
            return source.ToResult();

        var (transaction, _) = source.Value;
        var date = transaction.Date!.Value;
        var monthStart = new DateTime(date.Year, date.Month, 1);
        var inMonth = await _transactionRepository.GetTransactionsInRange(userId, monthStart, monthStart.AddMonths(1).AddDays(-1));

        var candidates = new List<Transaction>();
        foreach (var candidate in inMonth.Where(t => IsCounterpartShape(transaction, t)))
        {
            if (await IsFreeForTransfer(candidate, userId))
                candidates.Add(candidate);
        }

        // prima i movimenti con la data più vicina a quella di partenza
        var ordered = candidates
            .OrderBy(t => Math.Abs((t.Date!.Value - date).Ticks))
            .ThenBy(t => t.Id);
        return Result.Ok(_mapper.Map<IEnumerable<TransactionDto>>(ordered));
    }

    public async Task<Result> LinkCounterpart(int transactionId, int counterpartId, int userId)
    {
        var source = await LoadIncompleteLeg(transactionId, userId);
        if (source.IsFailed)
            return source.ToResult();

        if (counterpartId <= 0)
            return Result.Fail(DomainErrors.InvalidId("counterpart", counterpartId));

        var (transaction, transfer) = source.Value;
        var counterpart = (await _transactionRepository.GetTransactionsByIds([counterpartId], userId)).FirstOrDefault();
        if (counterpart is null)
            return Result.Fail(DomainErrors.NotFound("Transaction", counterpartId));

        if (!IsCounterpartShape(transaction, counterpart))
            return Result.Fail(DomainErrors.BadRequest(
                "The counterpart must be on another account, with the same absolute amount, opposite sign and a date in the same month."));

        if (!await IsFreeForTransfer(counterpart, userId))
            return Result.Fail(DomainErrors.Conflict($"Transaction {counterpartId} already belongs to a complete transfer."));

        var now = DateTime.UtcNow;
        counterpart.Date = transaction.Date;
        counterpart.UpdatedOn = now;

        if (transfer is null)
        {
            // movimento "Money transfers" senza trasferimento: si entra in quello del corrispondente, se ne ha uno,
            // altrimenti si crea il trasferimento e lo si assegna a entrambi
            if (counterpart.TransferId is int counterpartTransferId)
            {
                transaction.TransferId = counterpartTransferId;
            }
            else
            {
                var created = new Transfer { CreatedOn = now };
                await _repository.AddTransfer(created);
                transaction.TransferNavigation = created;
                counterpart.TransferNavigation = created;
            }

            transaction.UpdatedOn = now;
            await _repository.SaveChangesAsync();
            return Result.Ok();
        }

        // il trasferimento da cui proviene il candidato, se conteneva solo lui, resterebbe vuoto: va eliminato
        int? emptiedTransferId = null;
        if (counterpart.TransferId is int previousId)
        {
            var previous = await _repository.GetTransfer(previousId, userId);
            if (previous is not null && previous.Transactions.All(t => t.Id == counterpart.Id))
                emptiedTransferId = previousId;
        }

        counterpart.TransferId = transfer.Id;
        await _repository.SaveChangesAsync();

        // eliminato dopo lo spostamento, così il SetNull sulla FK non tocca il movimento appena collegato
        if (emptiedTransferId is int emptiedId)
        {
            await _repository.DeleteTransfer(emptiedId);
            await _repository.SaveChangesAsync();
        }

        return Result.Ok();
    }

    // Movimento collegato a un trasferimento a cui manca il ramo di segno opposto, oppure nella categoria
    // "Money transfers" senza trasferimento (Transfer null).
    private async Task<Result<(Transaction Transaction, Transfer? Transfer)>> LoadIncompleteLeg(int transactionId, int userId)
    {
        if (transactionId <= 0)
            return Result.Fail(DomainErrors.InvalidId("transaction", transactionId));

        var transaction = await _transactionRepository.GetTransaction(transactionId, userId);
        if (transaction is null)
            return Result.Fail(DomainErrors.NotFound("Transaction", transactionId));

        if (transaction.Date is null || transaction.Amount == 0m)
            return Result.Fail(DomainErrors.BadRequest($"Transaction {transactionId} needs a date and a non-zero amount."));

        if (transaction.TransferId is not int transferId)
        {
            var moneyTransfersId = await _categoryRepository.GetCategoryIdByKind(userId, CategoryEnum.MoneyTransfers);
            if (moneyTransfersId is null || transaction.CategoryId != moneyTransfersId)
                return Result.Fail(DomainErrors.BadRequest(
                    $"Transaction {transactionId} is neither linked to a transfer nor in the money transfers category."));

            return Result.Ok<(Transaction, Transfer?)>((transaction, null));
        }

        var transfer = await _repository.GetTransfer(transferId, userId);
        if (transfer is null)
            return Result.Fail(DomainErrors.NotFound("Transfer", transferId));

        if (HasOppositeLeg(transfer, transaction))
            return Result.Fail(DomainErrors.Conflict($"Transfer {transferId} already has both legs."));

        return Result.Ok<(Transaction, Transfer?)>((transaction, transfer));
    }

    private static bool IsCounterpartShape(Transaction source, Transaction candidate)
        => candidate.Id != source.Id
           && candidate.AccountId != source.AccountId
           && candidate.Amount == -source.Amount
           && candidate.Date is DateTime date
           && date.Year == source.Date!.Value.Year
           && date.Month == source.Date.Value.Month
           && (source.TransferId is null || candidate.TransferId != source.TransferId);

    // Un candidato è libero se non è in un trasferimento, o se il suo trasferimento non ha ancora il ramo opposto al suo.
    private async Task<bool> IsFreeForTransfer(Transaction candidate, int userId)
    {
        if (candidate.TransferId is not int transferId)
            return true;

        var transfer = await _repository.GetTransfer(transferId, userId);
        return transfer is null || !HasOppositeLeg(transfer, candidate);
    }

    private static bool HasOppositeLeg(Transfer transfer, Transaction leg)
        => transfer.Transactions.Any(t => t.Id != leg.Id && Math.Sign(t.Amount) == -Math.Sign(leg.Amount));

    private async Task<Result> ValidateTransferRequest(TransferDto dto, int userId)
    {
        if (dto.FromAccountId <= 0)
            return Result.Fail(DomainErrors.InvalidId("from account", dto.FromAccountId));

        if (dto.ToAccountId <= 0)
            return Result.Fail(DomainErrors.InvalidId("to account", dto.ToAccountId));

        if (dto.FromAccountId == dto.ToAccountId)
            return Result.Fail(DomainErrors.BadRequest("Source and destination accounts must be different."));

        if (dto.Amount <= 0)
            return Result.Fail(DomainErrors.BadRequest("Amount must be greater than zero."));

        var fromAccount = await _accountRepository.GetAccount(dto.FromAccountId, userId);
        if (fromAccount is null)
            return Result.Fail(DomainErrors.NotFound("Account", dto.FromAccountId));

        var toAccount = await _accountRepository.GetAccount(dto.ToAccountId, userId);
        if (toAccount is null)
            return Result.Fail(DomainErrors.NotFound("Account", dto.ToAccountId));

        return Result.Ok();
    }
}