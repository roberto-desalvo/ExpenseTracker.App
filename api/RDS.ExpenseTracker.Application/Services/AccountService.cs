using AutoMapper;
using FluentResults;
using RDS.ExpenseTracker.Application.Utilities;
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

    public async Task<Result<PagedResult<AccountDto>>> GetAccounts(AccountQueryRequest request, int userId)
    {
        var (items, totalCount) = await _repository.GetPagedAccounts(request, userId);
        return Result.Ok(new PagedResult<AccountDto>
        {
            Items = _mapper.Map<IEnumerable<AccountDto>>(items),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        });
    }

    public async Task<Result<AccountDto?>> GetAccount(int id, int userId)
    {
        if (id <= 0)
            return Result.Fail(DomainErrors.InvalidId("account", id));

        var account = await _repository.GetAccount(id, userId);
        if (account is null)
            return Result.Fail(DomainErrors.NotFound("Account", id));

        return Result.Ok(_mapper.Map<AccountDto?>(account));
    }

    public async Task<Result<decimal>> GetAvailability(int accountId, int userId)
    {
        if (accountId <= 0)
            return Result.Fail(DomainErrors.InvalidId("account", accountId));

        var account = await _repository.GetAccount(accountId, userId);
        if (account is null)
            return Result.Fail(DomainErrors.NotFound("Account", accountId));

        var availability = await _repository.GetAvailability(accountId, userId);
        return Result.Ok(availability);
    }

    public async Task<Result> AddAccounts(IEnumerable<AccountDto> dtos, int userId)
    {
        if (dtos is null || !dtos.Any())
            return Result.Fail(DomainErrors.Required("accounts"));

        var entities = _mapper.Map<IEnumerable<Account>>(dtos).ToList();
        foreach (var entity in entities)
        {
            entity.UserId = userId;
        }

        var ibanCheck = await NormalizeAndValidateIbans(entities, userId);
        if (ibanCheck.IsFailed)
            return ibanCheck;

        await _repository.AddAccounts(entities);
        await _repository.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> UpdateAccount(AccountDto dto, int userId)
    {
        if (dto.Id <= 0)
            return Result.Fail(DomainErrors.InvalidId("account", dto.Id));

        var current = await _repository.GetAccount(dto.Id, userId);
        if (current is null)
            return Result.Fail(DomainErrors.NotFound("Account", dto.Id));

        var entity = _mapper.Map<Account>(dto);
        entity.UserId = userId;

        var ibanCheck = await NormalizeAndValidateIbans([entity], userId);
        if (ibanCheck.IsFailed)
            return ibanCheck;

        await _repository.UpdateAccount(entity);
        await _repository.SaveChangesAsync();
        return Result.Ok();
    }

    // L'IBAN è opzionale; se presente viene normalizzato e deve essere unico tra i conti dell'utente,
    // perché gli import lo usano per riconoscere il conto controparte di un trasferimento.
    private async Task<Result> NormalizeAndValidateIbans(IReadOnlyCollection<Account> accounts, int userId)
    {
        foreach (var account in accounts)
        {
            account.Iban = IbanUtilities.Normalize(account.Iban);
            if (account.Iban is not null && !IbanUtilities.IsValid(account.Iban))
                return Result.Fail(DomainErrors.BadRequest($"IBAN '{account.Iban}' is not valid."));
        }

        var duplicateInRequest = accounts
            .Where(a => a.Iban is not null)
            .GroupBy(a => a.Iban)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateInRequest is not null)
            return Result.Fail(DomainErrors.Conflict($"IBAN '{duplicateInRequest.Key}' is assigned to more than one account."));

        var existing = (await _repository.GetAccounts(userId)).ToList();
        foreach (var account in accounts.Where(a => a.Iban is not null))
        {
            var owner = existing.FirstOrDefault(e => e.Iban == account.Iban && e.Id != account.Id);
            if (owner is not null)
                return Result.Fail(DomainErrors.Conflict($"IBAN '{account.Iban}' is already assigned to account '{owner.Name}'."));
        }

        return Result.Ok();
    }
}
