using AutoMapper;
using RDS.ExpenseTracker.Domain.Models;
using RDS.ExpenseTracker.DataImport.Business.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RDS.ExpenseTracker.DataImport.DataAccess.Repositories.Abstractions;
using RDS.ExpenseTracker.Api.Dtos;
using Microsoft.Extensions.Logging;

namespace RDS.ExpenseTracker.DataImport.Business.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IMapper _mapper;
        private readonly ICategoryRepository _repository;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(IMapper mapper, ICategoryRepository repository, ILogger<CategoryService> logger)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<Category>> GetCategories()
        {
            try
            {
                var dtos = await _repository.GetAllAsync();
                var categories = _mapper.Map<IEnumerable<Category>>(dtos);
                _logger.LogInformation("Successfully fetched categories");
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching categories");
                throw;
            }
        }


        public async Task<Category> GetDefaultCategory()
        {
            try
            {
                var entity = await _repository.GetDefaultCategory();
                var category = _mapper.Map<Category>(entity);
                _logger.LogInformation("Successfully fetched default category");
                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching default category");
                throw;
            }
        }
    }
}
