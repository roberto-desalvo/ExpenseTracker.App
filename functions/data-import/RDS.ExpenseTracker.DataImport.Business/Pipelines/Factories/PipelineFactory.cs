using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.DataImport.Business.Services.Abstractions;
using RDS.ExpenseTracker.DataImport.Business.Helpers;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Abstractions;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.ExcelDataExtraction;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.TransactionEnrichment;
using RDS.ExpenseTracker.Domain.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Factories.Abstractions;

namespace RDS.ExpenseTracker.DataImport.Business.Pipelines.Factories
{
    public class PipelineFactory : IPipelineFactory
    {
        private readonly ExpenseTrackerExcelOptions _config;
        private readonly ICategoryService _categoryService;
        private readonly IFinancialAccountService _financialAccountService;
        private readonly ITransactionService _transactionService;
        private readonly ILogger<Pipeline<Transaction>> _transactionPipelineLogger;
        private readonly ILogger<Pipeline<IFormFile>> _dataExtractionPipelineLogger;
        private readonly ILogger<PipelineFactory> _factoryLogger;
        private readonly ILogger<AssignAccountStep> _accountStepLogger;

        public PipelineFactory(
                IOptions<ExpenseTrackerExcelOptions> config,
                ICategoryService categoryService,
                IFinancialAccountService financialAccountService,
                ITransactionService transactionService,
                ILogger<PipelineFactory> factoryLogger,
                ILogger<Pipeline<Transaction>> transactionPipeLogger,
                ILogger<Pipeline<IFormFile>> extractionPipeLogger,
                ILogger<AssignAccountStep> accountStepLogger
            )
        {
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _financialAccountService = financialAccountService ?? throw new ArgumentNullException(nameof(financialAccountService));
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _dataExtractionPipelineLogger = extractionPipeLogger ?? throw new ArgumentNullException(nameof(extractionPipeLogger));
            _transactionPipelineLogger = transactionPipeLogger ?? throw new ArgumentNullException(nameof(transactionPipeLogger));
            _factoryLogger = factoryLogger ?? throw new ArgumentNullException(nameof(factoryLogger));
            _accountStepLogger = accountStepLogger ?? throw new ArgumentNullException(nameof(accountStepLogger));
        }

        public IPipeline<Transaction, Transaction> CreateTransactionEnrichmentPipeline(TransactionEnrichmentCtorArgs args)
        {
            try
            {
                var categoryStep = new AssignCategoryStep(args.Categories, args.DefaultCategory);
                var accountStep = new AssignAccountStep(args.Accounts, _accountStepLogger);
                var dateStep = new AssignDateStep(args.DefaultDate);

                return new Pipeline<Transaction>(_transactionPipelineLogger)
                    .AddStep(categoryStep)
                    .AddStep(accountStep)
                    .AddStep(dateStep);
            }
            catch (Exception ex)
            {
                _factoryLogger.LogError(ex, "Error occurred while instantiating transaction enrichment pipeline");
                throw;
            }
        }

        public IPipeline<IFormFile, IList<Transaction>> CreateExcelDataExtractionPipeline(bool importAll)
        {
            try
            {
                var readExcelFileStep = new ReadExcelFileStep();
                var getDataTablesStep = new GetDataTablesStep(_config.SheetsToIgnore, _transactionService, importAll);
                var filterTransactionsStep = new FilterTransactionsStep(_transactionService);
                var getTransactionsStep = new GetTransactionsStep(_config);
                var processTransactionStep = new ProcessTransactionsStep(_categoryService, _financialAccountService, this);

                return importAll 
                    ? new Pipeline<IFormFile>(_dataExtractionPipelineLogger)
                    .AddStep(readExcelFileStep)
                    .AddStep(getDataTablesStep)
                    .AddStep(getTransactionsStep)
                    .AddStep(processTransactionStep)
                    : new Pipeline<IFormFile>(_dataExtractionPipelineLogger)
                    .AddStep(readExcelFileStep)
                    .AddStep(getDataTablesStep)
                    .AddStep(getTransactionsStep)
                    .AddStep(filterTransactionsStep)
                    .AddStep(processTransactionStep);
            }
            catch (Exception ex)
            {
                _factoryLogger.LogError(ex, "Error occurred while instantiating data extraction pipeline");
                throw;
            }
        }
    }
}
