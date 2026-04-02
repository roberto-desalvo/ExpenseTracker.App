using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.DataImport.Business.Helpers;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.Abstractions;
using RDS.ExpenseTracker.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.TransactionEnrichment
{
    public class AssignCategoryStep : IPipelineStep<Transaction>
    {
        private readonly IList<Category> _categories;
        private readonly Category _defaultCategory;

        public AssignCategoryStep(IList<Category> categories) : this(categories, null)
        { }

        public AssignCategoryStep(IList<Category> categories, Category defaultCategory)
        {
            _categories = categories?.OrderBy(x => x.Priority, Comparer<int>.Default).ToList() ?? throw new ArgumentNullException(nameof(categories));
            _defaultCategory = defaultCategory;
        }

        public Task<Transaction> ProcessAsync(Transaction transaction)
        {
            foreach (var category in _categories)
            {
                // TODO move this to tag sanitization when saving categories
                var tags = category.Tags.Select(tag => tag.Trim()).Where(tag => !string.IsNullOrWhiteSpace(tag)).ToArray();

                if (transaction.Description.ContainsOne(ignoreCase: true, tags))
                {
                    transaction.CategoryId = category.Id;
                    break;
                }
            }

            if (transaction.CategoryId == 0 && _defaultCategory != null)
            {
                transaction.CategoryId = _defaultCategory.Id;
            }

            return Task.FromResult(transaction);
        }
    }
}
