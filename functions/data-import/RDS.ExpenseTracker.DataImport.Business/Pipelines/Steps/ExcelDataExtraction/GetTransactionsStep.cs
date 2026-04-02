using RDS.ExpenseTracker.DataImport.Business.Helpers;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.Abstractions;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.IO;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Utilities;
using RDS.ExpenseTracker.Domain.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.ExcelDataExtraction
{
    public class GetTransactionsStep : IPipelineStep<List<DataTable>, IList<TransactionsBySheet>>
    {
        private readonly ExpenseTrackerExcelOptions _config;

        public GetTransactionsStep(ExpenseTrackerExcelOptions config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<IList<TransactionsBySheet>> ProcessAsync(List<DataTable> dataTables)
        {
            try
            {
                var transactionsBySheet = new List<TransactionsBySheet>();
                var transactionsLock = new object();

                Parallel.ForEach(dataTables, current =>
                {
                    var transactions = GetTransactions(current);
                    var sheetDate = SheetHelper.ParseDateFromSheetName(current.TableName);
                    var output = new TransactionsBySheet(current.TableName, sheetDate, transactions);

                    lock (transactionsLock)
                    {
                        transactionsBySheet.Add(output);
                    }
                });
                return transactionsBySheet;
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting transactions", ex);
            }

        }

        public IEnumerable<Transaction> GetTransactions(DataTable dataTable)
        {
            var dataRows = dataTable.Rows.Cast<DataRow>();
            var transactions = dataRows.SelectMany(GetTransactionsFromRow);
            return transactions;
        }

        public IEnumerable<Transaction> GetTransactionsFromRow(DataRow row)
        {
            var model = ExcelDataRowHelper.GetDataRowModel(row, _config);

            if (model.TransactionAmount != 0 && !string.IsNullOrWhiteSpace(model.TransactionAccountName))
            {
                yield return ExcelDataRowHelper.ExtractStandardTransaction(model);
            }

            if (model.TransferAmount != 0 && !string.IsNullOrWhiteSpace(model.TransferDescription))
            {
                yield return ExcelDataRowHelper.ExtractOutgoingTransfer(model);
                yield return ExcelDataRowHelper.ExtractIngoingTransfer(model);
            }
        }
    }
}
