using RDS.ExpenseTracker.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.Domain.Repositories;

public interface ICategoryRepository : IRepositoryBase
{
    Task<Category?> GetCategory(int id);
    Task<Category?> GetDefaultCategory();
    Task<IEnumerable<Category>> GetCategories();
    Task AddCategories(IEnumerable<Category> categories);
    Task RemoveCategory(int id);
    Task RemoveCategory(Category category);
}
