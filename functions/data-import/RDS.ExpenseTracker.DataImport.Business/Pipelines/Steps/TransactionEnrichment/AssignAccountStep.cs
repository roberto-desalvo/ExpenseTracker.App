
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.Abstractions;
using RDS.ExpenseTracker.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.TransactionEnrichment
{
    public class AssignAccountStep : IPipelineStep<Transaction>
    {
        private readonly IList<FinancialAccount> _accounts;
        private readonly ILogger<AssignAccountStep> _logger;

        public AssignAccountStep(IList<FinancialAccount> accounts, ILogger<AssignAccountStep> logger)
        {
            _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<Transaction> ProcessAsync(Transaction transaction)
        {
            if (transaction.FinancialAccountId <= 0)
            {
                var account = _accounts.FirstOrDefault(x => x.Name.ToLower().Trim() == transaction.FinancialAccountName.ToLower().Trim());
                if (account != null)
                {
                    transaction.FinancialAccountId = account.Id;
                }
                else
                {
                    _logger.LogWarning("Account not found for transaction with id {id} and name {name}", 
                        transaction.FinancialAccountId, transaction.FinancialAccountName);
                }
            }

            return Task.FromResult(transaction);
        }
    }
}
