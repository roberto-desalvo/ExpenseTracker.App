using AutoMapper;
using FluentResults;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Application.Services;

public class AccountTypeService : IAccountTypeService
{
    private readonly IAccountTypeRepository _repository;
    private readonly IMapper _mapper;

    public AccountTypeService(IAccountTypeRepository repository, IMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<Result<IEnumerable<AccountTypeDto>>> GetAccountTypes()
        => Result.Ok(_mapper.Map<IEnumerable<AccountTypeDto>>(await _repository.GetAccountTypes()));
}
