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

    public async Task<Result<UserDto?>> GetUserAsync(int id)
    {
        if (id <= 0)
            return Result.Fail(DomainErrors.InvalidId("user", id));

        var user = await _repository.GetById(id);
        if (user is null)
            return Result.Fail(DomainErrors.NotFound("User", id));

        return Result.Ok(_mapper.Map<UserDto?>(user));
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

    public async Task<Result<UserDto>> GetOrCreateUserForImportAsync(string oid, string? email, string? name)
    {
        if (string.IsNullOrWhiteSpace(oid))
            return Result.Fail(DomainErrors.Required("oid"));

        var byOid = await _repository.GetByAzureOid(oid);
        if (byOid is not null)
            return Result.Ok(_mapper.Map<UserDto>(byOid));

        var byApp = await _repository.GetByAppOid(oid);
        if (byApp is not null)
            return Result.Ok(_mapper.Map<UserDto>(byApp));

        if (string.IsNullOrWhiteSpace(email))
            return Result.Fail(DomainErrors.Unauthorized(
                "No user or associated app found for this identity, and no email available to provision a new one."));

        var user = await _repository.GetOrCreateUserAsync(oid, email);
        return Result.Ok(_mapper.Map<UserDto>(user));
    }
}
