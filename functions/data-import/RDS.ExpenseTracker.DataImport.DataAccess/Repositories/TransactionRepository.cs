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
    public class TransactionRepository : ITransactionRepository
    {
        private readonly IApiContext _apiContext;
        private readonly string _endpoint;
        public TransactionRepository(IApiContext apiContext, IOptions<ExpenseTrackerApiSettings> settings)
        {
            _apiContext = apiContext ?? throw new ArgumentNullException(nameof(apiContext));
            _endpoint = settings?.Value?.TransactionEndpoint ?? throw new ArgumentNullException(nameof(settings));
        }
        public Task AddAsync(IEnumerable<TransactionDto> entities) => _apiContext.PostAsync<IEnumerable<TransactionDto>>(_endpoint, entities);
        public Task DeleteAsync(int id) => _apiContext.DeleteAsync($"{_endpoint}/{id}");
        public Task<IEnumerable<TransactionDto>> GetAllAsync() => _apiContext.GetAsync<IEnumerable<TransactionDto>>(_endpoint);
        public Task<TransactionDto> GetByIdAsync(int id) => _apiContext.GetAsync<TransactionDto>($"{_endpoint}/{id}");
        public Task<TransactionDto> GetLatestAsync() => _apiContext.GetAsync<TransactionDto>($"{_endpoint}/latest");
        public Task UpdateAsync(TransactionDto entity) => _apiContext.PutAsync<TransactionDto>($"{_endpoint}/{entity.Id}", entity);
        public Task Reset(IEnumerable<TransactionDto> transactions) => _apiContext.PutAsync<IEnumerable<TransactionDto>>($"{_endpoint}", transactions);

        public Task<IEnumerable<TransactionDto>> GetAsync(DateTime fromDate, DateTime toDate) 
            => _apiContext.GetAsync<IEnumerable<TransactionDto>>($"{_endpoint}?fromDate={fromDate:yyyy-MM-ddTHH:mm:ssZ}&toDate={toDate:yyyy-MM-ddTHH:mm:ssZ}");

    }
}
