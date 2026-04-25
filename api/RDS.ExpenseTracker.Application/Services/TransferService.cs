using AutoMapper;
using FluentResults;
using RDS.ExpenseTracker.Api.Dtos;
using RDS.ExpenseTracker.Domain.Common;
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
    private readonly IMapper _mapper;

    public TransferService(
        ITransferRepository repository,
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        IMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<Result<IEnumerable<TransferDto>>> GetTransfers()
    {
        var transfers = await _repository.GetTransfers();
        return Result.Ok(_mapper.Map<IEnumerable<TransferDto>>(transfers));
    }

    public async Task<Result<TransferDto?>> GetTransfer(int id)
    {
        if (id <= 0)
            return Result.Fail(DomainErrors.InvalidId("transfer", id));

        var transfer = await _repository.GetTransfer(id);
        if (transfer is null)
            return Result.Fail(DomainErrors.NotFound("Transfer", id));

        return Result.Ok<TransferDto?>(_mapper.Map<TransferDto>(transfer));
    }

    public async Task<Result<int>> AddTransfer(TransferDto dto)
    {
        var validation = await ValidateTransferRequest(dto);
        if (validation.IsFailed)
            return Result.Fail(validation.Errors);

        var transfer = new Transfer
        {
            CreatedOn = DateTime.UtcNow
        };

        var transferDate = dto.Date ?? DateTime.UtcNow;

        var fromTransaction = new Transaction
        {
            AccountId = dto.FromAccountId,
            CategoryId = (int)CategoryEnum.MoneyTransfers,
            Amount = -Math.Abs(dto.Amount),
            Description = dto.Description,
            Date = transferDate,
            CreatedOn = DateTime.UtcNow,
            TransferNavigation = transfer
        };

        var toTransaction = new Transaction
        {
            AccountId = dto.ToAccountId,
            CategoryId = (int)CategoryEnum.MoneyTransfers,
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

    public async Task<Result> UpdateTransfer(TransferDto dto)
    {
        if (dto.Id <= 0)
            return Result.Fail(DomainErrors.InvalidId("transfer", dto.Id));

        var transfer = await _repository.GetTransfer(dto.Id);
        if (transfer is null)
            return Result.Fail(DomainErrors.NotFound("Transfer", dto.Id));

        var validation = await ValidateTransferRequest(dto);
        if (validation.IsFailed)
            return Result.Fail(validation.Errors);

        var existingTransactions = await _transactionRepository.GetTransactionsByTransferId(dto.Id);
        foreach (var transaction in existingTransactions)
        {
            await _transactionRepository.DeleteTransaction(transaction.Id);
        }

        var transferDate = dto.Date ?? DateTime.UtcNow;

        await _transactionRepository.AddTransactions([
            new Transaction
            {
                AccountId = dto.FromAccountId,
                CategoryId = (int)CategoryEnum.MoneyTransfers,
                Amount = -Math.Abs(dto.Amount),
                Description = dto.Description,
                Date = transferDate,
                CreatedOn = DateTime.UtcNow,
                TransferId = dto.Id
            },
            new Transaction
            {
                AccountId = dto.ToAccountId,
                CategoryId = (int)CategoryEnum.MoneyTransfers,
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

    public async Task<Result> DeleteTransfer(int id)
    {
        if (id <= 0)
            return Result.Fail(DomainErrors.InvalidId("transfer", id));

        var transfer = await _repository.GetTransfer(id);
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

    private async Task<Result> ValidateTransferRequest(TransferDto dto)
    {
        if (dto.FromAccountId <= 0)
            return Result.Fail(DomainErrors.InvalidId("from account", dto.FromAccountId));

        if (dto.ToAccountId <= 0)
            return Result.Fail(DomainErrors.InvalidId("to account", dto.ToAccountId));

        if (dto.FromAccountId == dto.ToAccountId)
            return Result.Fail(DomainErrors.BadRequest("Source and destination accounts must be different."));

        if (dto.Amount <= 0)
            return Result.Fail(DomainErrors.BadRequest("Amount must be greater than zero."));

        var fromAccount = await _accountRepository.GetAccount(dto.FromAccountId);
        if (fromAccount is null)
            return Result.Fail(DomainErrors.NotFound("Account", dto.FromAccountId));

        var toAccount = await _accountRepository.GetAccount(dto.ToAccountId);
        if (toAccount is null)
            return Result.Fail(DomainErrors.NotFound("Account", dto.ToAccountId));

        return Result.Ok();
    }
}