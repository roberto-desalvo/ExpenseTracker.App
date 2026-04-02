using Microsoft.AspNetCore.Http;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Abstractions;
using RDS.ExpenseTracker.Domain.Models;

namespace RDS.ExpenseTracker.DataImport.Business.Pipelines.Factories.Abstractions
{
    public interface IPipelineFactory
    {
        IPipeline<IFormFile, IList<Transaction>> CreateExcelDataExtractionPipeline(bool importAll);
        IPipeline<Transaction, Transaction> CreateTransactionEnrichmentPipeline(TransactionEnrichmentCtorArgs args);
    }
}