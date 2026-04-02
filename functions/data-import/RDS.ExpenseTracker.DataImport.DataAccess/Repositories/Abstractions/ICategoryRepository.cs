using RDS.ExpenseTracker.Api.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.DataAccess.Repositories.Abstractions
{
    public interface ICategoryRepository : IRepository<CategoryDto>
    {
        Task<CategoryDto> GetDefaultCategory();
    }
}
