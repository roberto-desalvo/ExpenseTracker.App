using Microsoft.Extensions.Options;
using RDS.ExpenseTracker.Api.Dtos;
using RDS.ExpenseTracker.DataImport.DataAccess.Context.Abstractions;
using RDS.ExpenseTracker.DataImport.DataAccess.Repositories.Abstractions;
using RDS.ExpenseTracker.DataImport.DataAccess.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.DataAccess.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly IApiContext _apiContext;
        private readonly string _endpoint;
        public CategoryRepository(IApiContext apiContext, IOptions<ExpenseTrackerApiSettings> settings)
        {
            _apiContext = apiContext ?? throw new ArgumentNullException(nameof(apiContext));
            _endpoint = settings?.Value?.CategoryEndpoint ?? throw new ArgumentNullException(nameof(settings));
        }
        public async Task AddAsync(IEnumerable<CategoryDto> entities) => await _apiContext.PostAsync<IEnumerable<CategoryDto>>(_endpoint, entities);

        public Task DeleteAsync(int id) => _apiContext.DeleteAsync($"{_endpoint}/{id}");

        public Task<IEnumerable<CategoryDto>> GetAllAsync() => _apiContext.GetAsync<IEnumerable<CategoryDto>>(_endpoint);

        public Task<CategoryDto> GetByIdAsync(int id) => _apiContext.GetAsync<CategoryDto>($"{_endpoint}/{id}");

        public Task UpdateAsync(CategoryDto entity) => _apiContext.PutAsync<CategoryDto>($"{_endpoint}/{entity.Id}", entity);

        public Task<CategoryDto> GetDefaultCategory() => _apiContext.GetAsync<CategoryDto>($"{_endpoint}/default");
    }
}
