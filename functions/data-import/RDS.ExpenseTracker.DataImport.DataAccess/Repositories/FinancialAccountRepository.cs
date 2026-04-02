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
    public class FinancialAccountRepository : IRepository<FinancialAccountDto>
    {
        private readonly IApiContext _apiContext;
        private readonly string _endpoint;
        public FinancialAccountRepository(IApiContext apiContext, IOptions<ExpenseTrackerApiSettings> settings)
        {
            _apiContext = apiContext ?? throw new ArgumentNullException(nameof(apiContext));
            _endpoint = settings?.Value?.AccountEndpoint ?? throw new ArgumentNullException(nameof(settings));
        }
        public Task AddAsync(IEnumerable<FinancialAccountDto> entities) => _apiContext.PostAsync<IEnumerable<FinancialAccountDto>>(_endpoint, entities);
        public Task DeleteAsync(int id) => _apiContext.DeleteAsync($"{_endpoint}/{id}");
        public Task<IEnumerable<FinancialAccountDto>> GetAllAsync() => _apiContext.GetAsync<IEnumerable<FinancialAccountDto>>(_endpoint);
        public Task<FinancialAccountDto> GetByIdAsync(int id) => _apiContext.GetAsync<FinancialAccountDto>($"{_endpoint}/{id}");
        public Task UpdateAsync(FinancialAccountDto entity) => _apiContext.PutAsync<FinancialAccountDto>($"{_endpoint}/{entity.Id}", entity);
    }
}
