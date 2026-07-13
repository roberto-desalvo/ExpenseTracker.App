using AutoMapper;
using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IMapper _mapper;

    public UserService(IUserRepository repository, IMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<Result<UserDto>> GetOrCreateUserAsync(string azureOid, string email, string? name)
    {
        if (string.IsNullOrWhiteSpace(azureOid))
            return Result.Fail(DomainErrors.Required("azureOid"));

        if (string.IsNullOrWhiteSpace(email))
            return Result.Fail(DomainErrors.Required("email"));

        // 'name' non viene persistito: lo schema Users non prevede una colonna Name.
        var user = await _repository.GetOrCreateUserAsync(azureOid, email);
        return Result.Ok(_mapper.Map<UserDto>(user));
    }
}
