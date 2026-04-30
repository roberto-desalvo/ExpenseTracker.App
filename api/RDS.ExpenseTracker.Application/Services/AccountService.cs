using AutoMapper;
using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Application.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _repository;
    private readonly IMapper _mapper;

    public AccountService(IAccountRepository repository, IMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<Result<PagedResult<AccountDto>>> GetAccounts(AccountQueryRequest request)
    {
        var (items, totalCount) = await _repository.GetPagedAccounts(request);
        return Result.Ok(new PagedResult<AccountDto>
        {
            Items = _mapper.Map<IEnumerable<AccountDto>>(items),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        });
    }

    public async Task<Result<AccountDto?>> GetAccount(int id)
    {
        if (id <= 0)
            return Result.Fail(DomainErrors.InvalidId("account", id));

        var account = await _repository.GetAccount(id);
        if (account is null)
            return Result.Fail(DomainErrors.NotFound("Account", id));

        return Result.Ok(_mapper.Map<AccountDto?>(account));
    }

    public async Task<Result<decimal>> GetAvailability(int accountId)
    {
        if (accountId <= 0)
            return Result.Fail(DomainErrors.InvalidId("account", accountId));

        var account = await _repository.GetAccount(accountId);
        if (account is null)
            return Result.Fail(DomainErrors.NotFound("Account", accountId));

        var availability = await _repository.GetAvailability(accountId);
        return Result.Ok(availability);
    }

    public async Task<Result> AddAccounts(IEnumerable<AccountDto> dtos)
    {
        if (dtos is null || !dtos.Any())
            return Result.Fail(DomainErrors.Required("accounts"));

        var entities = _mapper.Map<IEnumerable<Account>>(dtos);
        await _repository.AddAccounts(entities);
        await _repository.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> UpdateAccount(AccountDto dto)
    {
        if (dto.Id <= 0)
            return Result.Fail(DomainErrors.InvalidId("account", dto.Id));

        var current = await _repository.GetAccount(dto.Id);
        if (current is null)
            return Result.Fail(DomainErrors.NotFound("Account", dto.Id));

        var entity = _mapper.Map<Account>(dto);
        await _repository.UpdateAccount(entity);
        await _repository.SaveChangesAsync();
        return Result.Ok();
    }

}
