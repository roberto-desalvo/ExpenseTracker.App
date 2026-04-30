using AutoMapper;
using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;
using System.Globalization;

namespace RDS.ExpenseTracker.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repository;
    private readonly IAccountRepository _accountRepository;
    private readonly IMapper _mapper;

    public TransactionService(
        ITransactionRepository repository,
        IAccountRepository accountRepository,
        IMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<Result<TransactionQueryResult>> GetPagedTransactions(TransactionQueryRequest request)
    {
        var (items, totalCount, totalIncomes, totalOutcomes, totalNet) = await _repository.GetPagedTransactions(request);
        return Result.Ok(new TransactionQueryResult
        {
            Items = _mapper.Map<IEnumerable<TransactionDto>>(items),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalIncomes = totalIncomes,
            TotalOutcomes = totalOutcomes,
            TotalNet = totalNet
        });
    }

    public async Task<Result<IEnumerable<TransactionMonthOptionDto>>> GetAvailableMonthOptions()
    {
        var cultureInfo = new CultureInfo("it-IT");
        var monthOptions = (await _repository.GetAvailableMonthRanges())
            .Select(range => new TransactionMonthOptionDto
            {
                StartDate = range.StartDate,
                EndDate = range.EndDate,
                Description = cultureInfo.TextInfo.ToTitleCase(
                    range.StartDate.ToString("MMMM yyyy", cultureInfo))
            });

        return Result.Ok(monthOptions);
    }

    public async Task<Result<IEnumerable<TransactionDto>>> GetTransactions(TransactionQueryRequest? filter = null)
    {
        var transactions = await _repository.GetTransactions();

        if (filter != null)
        {
            if (filter.FromDate.HasValue)
                transactions = transactions.Where(x => x.Date >= filter.FromDate);
            if (filter.ToDate.HasValue)
                transactions = transactions.Where(x => x.Date <= filter.ToDate);
        }

        return Result.Ok(_mapper.Map<IEnumerable<TransactionDto>>(transactions));
    }

    public async Task<Result<TransactionDto?>> GetTransaction(int id)
    {
        if (id <= 0)
            return Result.Fail(DomainErrors.InvalidId("transaction", id));

        var transaction = await _repository.GetTransaction(id);
        if (transaction is null)
            return Result.Fail(DomainErrors.NotFound("Transaction", id));

        return Result.Ok(_mapper.Map<TransactionDto?>(transaction));
    }

    public async Task<Result<TransactionDto?>> GetLatestTransaction()
    {
        var transaction = await _repository.GetLatestTransaction();
        if (transaction is null || transaction.Id <= 0)
            return Result.Fail(DomainErrors.NotFound("Latest transaction"));

        return Result.Ok(_mapper.Map<TransactionDto?>(transaction));
    }

    public async Task<Result> AddTransactions(IEnumerable<TransactionDto> dtos)
    {
        if (dtos is null || !dtos.Any())
            return Result.Fail(DomainErrors.Required("transactions"));

        var entities = _mapper.Map<IEnumerable<Transaction>>(dtos);
        await _repository.AddTransactions(entities);
        await _repository.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result<int>> AddTransaction(TransactionDto dto)
    {
        if (dto.AccountId <= 0)
            return Result.Fail(DomainErrors.InvalidId("account", dto.AccountId));

        var account = await _accountRepository.GetAccount(dto.AccountId);
        if (account is null)
            return Result.Fail(DomainErrors.NotFound("Account", dto.AccountId));

        var entity = _mapper.Map<Transaction>(dto);
        await _repository.AddTransactions([entity]);
        await _repository.SaveChangesAsync();
        return Result.Ok(entity.Id);
    }

    public async Task<Result> UpdateTransaction(TransactionDto dto)
    {
        if (dto.Id <= 0)
            return Result.Fail(DomainErrors.InvalidId("transaction", dto.Id));

        var existing = await _repository.GetTransaction(dto.Id);
        if (existing is null)
            return Result.Fail(DomainErrors.NotFound("Transaction", dto.Id));

        var entity = _mapper.Map<Transaction>(dto);
        await _repository.UpdateTransaction(entity);
        await _repository.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> DeleteTransaction(int id)
    {
        if (id <= 0)
            return Result.Fail(DomainErrors.InvalidId("transaction", id));

        var existing = await _repository.GetTransaction(id);
        if (existing is null)
            return Result.Fail(DomainErrors.NotFound("Transaction", id));

        await _repository.DeleteTransaction(id);
        await _repository.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> DeleteAllTransactions()
    {
        await _repository.DeleteAllTransactions();
        await _repository.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> ResetTransactions(IEnumerable<TransactionDto> dtos)
    {
        await DeleteAllTransactions();
        await AddTransactions(dtos);
        return Result.Ok();
    }
}
