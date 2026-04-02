using RDS.ExpenseTracker.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Business.Services.Abstractions
{
    public interface ICategoryService
    {
        Task<Category> GetDefaultCategory();
        Task<IEnumerable<Category>> GetCategories();
    }
}
