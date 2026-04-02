using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.Abstractions;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Utilities;
using RDS.ExpenseTracker.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.TransactionEnrichment
{
    public class AssignDateStep : IPipelineStep<Transaction>
    {
        private readonly DateTime _defaultDate;

        public AssignDateStep(DateTime defaultDate)
        {
            _defaultDate = defaultDate;
        }

        public Task<Transaction> ProcessAsync(Transaction transaction)
        {
            var registeredDate = transaction.Date;
            transaction.Date = registeredDate == null ? _defaultDate : new DateTime(_defaultDate.Year, registeredDate.Value.Month, registeredDate.Value.Day);
            return Task.FromResult(transaction);
        }
    }
}
