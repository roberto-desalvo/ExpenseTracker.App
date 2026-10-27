using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Infrastructure.Repositories;

public class CategoryRepository : RepositoryBase, ICategoryRepository
{
    public CategoryRepository(ExpenseTrackerContext context)
        : base(context)
    {
    }

    public async Task AddCategories(IEnumerable<Category> categories)
    {
        await Context.Categories.AddRangeAsync(categories);
    }

    public async Task UpdateCategory(Category category)
    {
        var current = await Context.Categories.FirstOrDefaultAsync(x => x.Id == category.Id);
        if (current is null)
        {
            return;
        }

        Context.Entry(current).CurrentValues.SetValues(category);
    }

    public async Task RemoveCategory(int id)
    {
        var entity = await Context.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (entity != null)
        {
            await RemoveCategory(entity);
        }
    }

    public Task RemoveCategory(Category category)
    {
        Context.Categories.Remove(category);
        return Task.CompletedTask;
    }

    public async Task DeleteCategories(int userId)
    {
        var categories = await Context.Categories
            .Where(c => c.UserId == userId)
            .ToListAsync();
        Context.Categories.RemoveRange(categories);
    }

    public async Task<IEnumerable<Category>> GetCategories(int userId, string? name = null)
    {
        var query = Context.Categories.Where(c => c.UserId == userId);

        if (!string.IsNullOrWhiteSpace(name))
        {
            var normalizedName = name.Trim();
            query = query.Where(c => EF.Functions.Like(c.Name, $"%{normalizedName}%"));
        }

        return await query
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedCategories(CategoryQueryRequest request, int userId)
    {
        var query = Context.Categories.Where(c => c.UserId == userId);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var normalizedName = request.Name.Trim();
            query = query.Where(c => EF.Functions.Like(c.Name, $"%{normalizedName}%"));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Name)
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Category?> GetCategory(int id, int userId)
    {
        return await Context.Categories.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
    }

    public async Task<Category?> GetDefaultCategory(int userId)
    {
        return await Context.Categories
            .Where(c => c.UserId == userId && c.IsDefault == true)
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<int?> GetCategoryIdByKind(int userId, CategoryEnum kind)
    {
        return await Context.Categories
            .Where(c => c.UserId == userId && c.Kind == kind)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();
    }

    public async Task ReassignTransactionsToCategory(int sourceCategoryId, int targetCategoryId)
    {
        await Context.Transactions
            .Where(t => t.CategoryId == sourceCategoryId)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(t => t.CategoryId, targetCategoryId)
                .SetProperty(t => t.UpdatedOn, DateTime.UtcNow));
    }
}
