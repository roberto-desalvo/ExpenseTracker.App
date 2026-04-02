using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.DataImport.Business.Services.Abstractions;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.Abstractions;
using RDS.ExpenseTracker.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Factories.Abstractions;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Factories;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.IO;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Utilities;
using System.Data;

namespace RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.ExcelDataExtraction
{
    public class ProcessTransactionsStep : IPipelineStep<IList<TransactionsBySheet>, IList<Transaction>>
    {
        private readonly ICategoryService _categoryService;
        private readonly IFinancialAccountService _financialAccountService;
        private readonly IPipelineFactory _pipelineFactory;

        public ProcessTransactionsStep(
            ICategoryService categoryService,
            IFinancialAccountService financialAccountService,
            IPipelineFactory pipelineFactory)
        {
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _financialAccountService = financialAccountService ?? throw new ArgumentNullException(nameof(financialAccountService));
            _pipelineFactory = pipelineFactory ?? throw new ArgumentNullException(nameof(pipelineFactory));
        }

        public async Task<IList<Transaction>> ProcessAsync(IList<TransactionsBySheet> transactionsBySheet)
        {
            IEnumerable<Category> categories = new List<Category>();
            var defaultCategory = new Category();
            IEnumerable<FinancialAccount> accounts = new List<FinancialAccount>();
            var accountNamesFound = transactionsBySheet.SelectMany(x => x.Transactions)
                .Select(x => x.FinancialAccountName)
                .Distinct()
                .ToList();

            var tasks = new List<Task>
            {
                Task.Run(async () => categories = await _categoryService.GetCategories()),
                Task.Run(async () => defaultCategory = await _categoryService.GetDefaultCategory()),
                Task.Run(async () => accounts = await GetOrCreateAccounts(accountNamesFound))
            };
            await Task.WhenAll(tasks);

            var output = new List<Transaction>();
            var outputLock = new object();

            Parallel.ForEach(transactionsBySheet, async current =>
            {
                var defaultDate = SheetHelper.ParseDateFromSheetName(current.SheetName);
                var pipelineArgs = new TransactionEnrichmentCtorArgs(categories.ToList(), accounts.ToList(), defaultCategory, defaultDate);
                var pipeline = _pipelineFactory.CreateTransactionEnrichmentPipeline(pipelineArgs);

                foreach (var transaction in current.Transactions)
                {
                    var processed = await pipeline.ProcessAsync(transaction);
                    lock (outputLock)
                    {
                        output.Add(processed);
                    }
                }
            });

            return output;
        }

        private async Task<IList<FinancialAccount>> GetOrCreateAccounts(IList<string> accountNamesFound)
        {
            var accounts = await _financialAccountService.GetFinancialAccounts();

            var missingAccountNames = accountNamesFound.Where(name => !accounts.Any(
                account => account.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            if (missingAccountNames.Any())
            {
                foreach (var name in missingAccountNames)
                {
                    var account = new FinancialAccount
                    {
                        Name = name,
                        Description = name,
                    };
                    await _financialAccountService.AddFinancialAccount(account);
                }
                accounts = await _financialAccountService.GetFinancialAccounts();
            }

            return accounts.ToList();
        }
    }
}
