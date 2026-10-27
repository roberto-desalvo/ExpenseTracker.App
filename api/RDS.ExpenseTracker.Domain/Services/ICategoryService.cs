using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;

namespace RDS.ExpenseTracker.Domain.Services;

public interface ICategoryService : IService
{
    Task<Result<PagedResult<CategoryDto>>> GetCategories(CategoryQueryRequest request, int userId);
    Task<Result<CategoryDto?>> GetCategory(int id, int userId);
    Task<Result<CategoryDto?>> GetDefaultCategory(int userId);
    Task<Result> AddCategories(IEnumerable<CategoryDto> categories, int userId);
    Task<Result> UpdateCategory(CategoryDto category, int userId);
    Task<Result> DeleteCategory(int id, int userId);
    Task<Result<CategorySyncResultDto>> SyncTransactionCategories(CategorySyncRequest request, int userId);
}
