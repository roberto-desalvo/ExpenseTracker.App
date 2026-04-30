using AutoMapper;
using FluentResults;
using RDS.ExpenseTracker.Api.Dtos;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;
    private readonly IMapper _mapper;

    public CategoryService(ICategoryRepository repository, IMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<Result<IEnumerable<CategoryDto>>> GetCategories(string? name = null)
    {
        var categories = await _repository.GetCategories(name);
        return Result.Ok(_mapper.Map<IEnumerable<CategoryDto>>(categories));
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
}
