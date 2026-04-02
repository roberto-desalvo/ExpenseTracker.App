using RDS.ExpenseTracker.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.Business.Services.Abstractions
{
    public interface ICategoryService
    {
        Task<Category> GetDefaultCategory();
        Task<IEnumerable<Category>> GetCategories();
        Task DeleteCategory(int id);
        Task UpdateCategory(Category category);
        Task AddCategories(IEnumerable<Category> categories);
        Task<Category?> GetCategory(int id);
    }
}
