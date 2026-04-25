using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Infrastructure.Repositories
{
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

        public async Task RemoveCategory(int id)
        {
            var entity = await Context.Categories.FirstOrDefaultAsync(x => x.Id == id);
            if (entity != null)
            {
                await RemoveCategory(entity);
            }
        }
        public async Task RemoveCategory(Category category)
        {
            Context.Categories.Remove(category);
        }

        public async Task<IEnumerable<Category>> GetCategories()
        {
            return await Context.Categories.ToListAsync();
        }

        public async Task<Category?> GetCategory(int id)
        {
            return await Context.Categories.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Category?> GetDefaultCategory()
        {
            return await Context.Categories.FirstOrDefaultAsync(c => c.IsDefault == true);
        }
    }
}
