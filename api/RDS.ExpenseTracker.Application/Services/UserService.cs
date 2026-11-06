using System.Text.Json;
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

    public async Task<Result<UserDto>> GetUserByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Fail(DomainErrors.Required("email"));

        var user = await _repository.GetByEmail(email.Trim());
        if (user is null)
            return Result.Fail(DomainErrors.Unauthorized($"No user found with email '{email}'."));

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

    public async Task<Result<AnalyticsLayoutDto?>> GetAnalyticsLayoutAsync(int userId)
    {
        var user = await _repository.GetById(userId);
        if (user is null)
            return Result.Fail(DomainErrors.NotFound("User", userId));

        if (string.IsNullOrWhiteSpace(user.AnalyticsLayout))
            return Result.Ok<AnalyticsLayoutDto?>(null);

        try
        {
            return Result.Ok(JsonSerializer.Deserialize<AnalyticsLayoutDto>(user.AnalyticsLayout, JsonOptions));
        }
        catch (JsonException)
        {
            // JSON corrotto: si torna al layout predefinito invece di bloccare la pagina.
            return Result.Ok<AnalyticsLayoutDto?>(null);
        }
    }

    public async Task<Result<AnalyticsLayoutDto>> SaveAnalyticsLayoutAsync(int userId, AnalyticsLayoutDto layout)
    {
        var validation = ValidateAnalyticsLayout(layout);
        if (validation is not null)
            return Result.Fail(validation);

        var user = await _repository.GetById(userId);
        if (user is null)
            return Result.Fail(DomainErrors.NotFound("User", userId));

        var normalized = new AnalyticsLayoutDto
        {
            Cards = [.. layout.Cards.Select(c => new AnalyticsCardLayoutDto { Id = c.Id.Trim(), Width = c.Width })],
        };
        user.AnalyticsLayout = JsonSerializer.Serialize(normalized, JsonOptions);
        await _repository.SaveChangesAsync();
        return Result.Ok(normalized);
    }

    public async Task<Result<CurrentUserDto>> GetCurrentUserAsync(int userId)
    {
        var user = await _repository.GetById(userId);
        if (user is null)
            return Result.Fail(DomainErrors.NotFound("User", userId));

        return Result.Ok(new CurrentUserDto { Email = user.Email, IsDemo = user.IsDemo, IsAdmin = user.IsAdmin });
    }

    public async Task<Result> EnsureAdminAsync(int userId)
    {
        var user = await _repository.GetById(userId);
        if (user is null)
            return Result.Fail(DomainErrors.NotFound("User", userId));

        return user.IsAdmin
            ? Result.Ok()
            : Result.Fail(DomainErrors.Forbidden("This operation is reserved to administrators."));
    }

    private static BadRequestResultError? ValidateAnalyticsLayout(AnalyticsLayoutDto? layout)
    {
        if (layout?.Cards is null)
            return DomainErrors.BadRequest("cards is required.");

        if (layout.Cards.Count > MaxAnalyticsCards)
            return DomainErrors.BadRequest($"At most {MaxAnalyticsCards} cards are allowed.");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var card in layout.Cards)
        {
            if (card is null || string.IsNullOrWhiteSpace(card.Id) || card.Id.Trim().Length > MaxAnalyticsCardIdLength)
                return DomainErrors.BadRequest($"Every card needs an id of at most {MaxAnalyticsCardIdLength} characters.");

            if (!ids.Add(card.Id.Trim()))
                return DomainErrors.BadRequest($"Card '{card.Id}' appears more than once.");

            if (!AnalyticsCardWidths.Contains(card.Width))
                return DomainErrors.BadRequest(
                    $"Invalid width '{card.Width}' for card '{card.Id}' (allowed: {string.Join(", ", AnalyticsCardWidths)}).");
        }

        return null;
    }

    private const int MaxAnalyticsCards = 20;
    private const int MaxAnalyticsCardIdLength = 50;
    private static readonly string[] AnalyticsCardWidths = ["third", "half", "twoThirds", "full"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
