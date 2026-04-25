using FluentResults;
using RDS.ExpenseTracker.Api.Dtos;
using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Domain.Services;

public interface ICategoryService : IService
{
    Task<Result<IEnumerable<CategoryDto>>> GetCategories();
    Task<Result<CategoryDto?>> GetCategory(int id);
    Task<Result<CategoryDto?>> GetDefaultCategory();
    Task<Result> AddCategories(IEnumerable<CategoryDto> categories);
    Task<Result> UpdateCategory(CategoryDto category);
    Task<Result> DeleteCategory(int id);
}
