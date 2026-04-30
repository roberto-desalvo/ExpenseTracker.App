using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;

namespace RDS.ExpenseTracker.Domain.Services;

public interface ICategoryService : IService
{
    Task<Result<PagedResult<CategoryDto>>> GetCategories(CategoryQueryRequest request);
    Task<Result<CategoryDto?>> GetCategory(int id);
    Task<Result<CategoryDto?>> GetDefaultCategory();
    Task<Result> AddCategories(IEnumerable<CategoryDto> categories);
    Task<Result> UpdateCategory(CategoryDto category);
    Task<Result> DeleteCategory(int id);
}
