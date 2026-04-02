using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.Abstractions;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.IO;
using RDS.ExpenseTracker.DataImport.Business.Services.Abstractions;
using RDS.ExpenseTracker.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.ExcelDataExtraction
{
    public class FilterTransactionsStep : IPipelineStep<IList<TransactionsBySheet>, IList<TransactionsBySheet>>
    {
        private readonly ITransactionService _transactionService;

        public FilterTransactionsStep(ITransactionService transactionService) 
        {
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
        }

        public async Task<IList<TransactionsBySheet>> ProcessAsync(IList<TransactionsBySheet> transactionsBySheet)
        {
            var latestTransaction = await _transactionService.GetLatestAsync();
            var latestTransactionDate = latestTransaction?.Date;
            if(latestTransactionDate == null)
            {
                return transactionsBySheet;
            }

            foreach (var sheetTransactions in transactionsBySheet) 
            { 
                if(sheetTransactions.SheetDate.Month == latestTransactionDate.Value.Month && sheetTransactions.SheetDate.Year == latestTransactionDate.Value.Year)
                {
                    var transactionsInSameDay = (await _transactionService.GetAsync(latestTransactionDate.Value.AddDays(-1), latestTransactionDate.Value.AddDays(1))).ToList();

                    var newTransactions = new List<Transaction>();

                    foreach (var transaction in sheetTransactions.Transactions)
                    {
                        if(transaction.Date > latestTransactionDate)
                        {
                            newTransactions.Add(transaction);
                        }

                        if(transaction.Date == latestTransactionDate)
                        {
                            var match = transactionsInSameDay.FirstOrDefault(x 
                                => x.Description == transaction.Description 
                                && x.FinancialAccountName == transaction.FinancialAccountName
                                && x.Amount == transaction.Amount);

                            if(match != null)
                            {
                                transactionsInSameDay.Remove(match);
                            }
                            else
                            {
                                newTransactions.Add(transaction);
                            }
                        }
                    }
                    sheetTransactions.Transactions = newTransactions;
                }
            }

            return transactionsBySheet;
        }
    }
}
