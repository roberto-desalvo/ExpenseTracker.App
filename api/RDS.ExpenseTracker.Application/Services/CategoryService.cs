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

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IMapper _mapper;

    public CategoryService(ICategoryRepository repository, ITransactionRepository transactionRepository, IMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<Result<PagedResult<CategoryDto>>> GetCategories(CategoryQueryRequest request)
    {
        var (items, totalCount) = await _repository.GetPagedCategories(request);
        return Result.Ok(new PagedResult<CategoryDto>
        {
            Items = _mapper.Map<IEnumerable<CategoryDto>>(items),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        });
    }

    public async Task<Result<CategoryDto?>> GetCategory(int id)
    {
        if (id <= 0)
            return Result.Fail(DomainErrors.InvalidId("category", id));

        var category = await _repository.GetCategory(id);
        if (category is null)
            return Result.Fail(DomainErrors.NotFound("Category", id));

        return Result.Ok(_mapper.Map<CategoryDto?>(category));
    }

    public async Task<Result<CategoryDto?>> GetDefaultCategory()
    {
        var category = await _repository.GetDefaultCategory();
        if (category is null)
            return Result.Fail(DomainErrors.NotFound("Default category"));

        return Result.Ok(_mapper.Map<CategoryDto?>(category));
    }

    public async Task<Result> AddCategories(IEnumerable<CategoryDto> categories)
    {
        if (categories is null || !categories.Any())
            return Result.Fail(DomainErrors.Required("categories"));

        var entities = _mapper.Map<IEnumerable<Category>>(categories);
        await _repository.AddCategories(entities);
        await _repository.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> UpdateCategory(CategoryDto category)
    {
        if (category.Id <= 0)
            return Result.Fail(DomainErrors.InvalidId("category", category.Id));

        var current = await _repository.GetCategory(category.Id);
        if (current is null)
            return Result.Fail(DomainErrors.NotFound("Category", category.Id));

        var entity = _mapper.Map<Category>(category);
        await _repository.UpdateCategory(entity);
        await _repository.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> DeleteCategory(int id)
    {
        if (id <= 0)
            return Result.Fail(DomainErrors.InvalidId("category", id));

        var current = await _repository.GetCategory(id);
        if (current is null)
            return Result.Fail(DomainErrors.NotFound("Category", id));

        if (current.IsDefault == true)
            return Result.Fail(DomainErrors.BadRequest("Cannot delete default category."));

        var defaultCategory = await _repository.GetDefaultCategory();
        if (defaultCategory is null)
            return Result.Fail(DomainErrors.NotFound("Default category"));

        await _repository.ReassignTransactionsToCategory(id, defaultCategory.Id);
        await _repository.RemoveCategory(id);
        await _repository.SaveChangesAsync();
        return Result.Ok();
    }

    // Riapplica i tag delle categorie ai movimenti già salvati, con la stessa regola dell'import.
    // I trasferimenti restano intatti e i movimenti senza tag corrispondente mantengono la categoria attuale.
    public async Task<Result<CategorySyncResultDto>> SyncTransactionCategories(CategorySyncRequest request, int userId)
    {
        if (request?.StartDate is null)
            return Result.Fail(DomainErrors.Required("startDate"));
        if (request.EndDate is null)
            return Result.Fail(DomainErrors.Required("endDate"));
        if (request.StartDate.Value.Date > request.EndDate.Value.Date)
            return Result.Fail(DomainErrors.BadRequest("startDate must not be after endDate."));

        var matcher = new CategoryTagMatcher(await _repository.GetCategories());
        var transactions = await _transactionRepository.GetCategorizableTransactions(userId, request.StartDate.Value, request.EndDate.Value);

        var result = new CategorySyncResultDto { ExaminedTransactions = transactions.Count };
        var now = DateTime.UtcNow;

        foreach (var transaction in transactions)
        {
            var categoryId = matcher.Match(transaction.Description);
            if (categoryId is null)
            {
                result.UnmatchedTransactions++;
                continue;
            }

            if (transaction.CategoryId == categoryId)
                continue;

            transaction.CategoryId = categoryId;
            transaction.UpdatedOn = now;
            result.UpdatedTransactions++;
        }

        if (result.UpdatedTransactions > 0)
            await _transactionRepository.SaveChangesAsync();

        return Result.Ok(result);
    }
}
