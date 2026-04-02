using RDS.ExpenseTracker.DataImport.Business.Helpers;
using RDS.ExpenseTracker.Domain.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Business.Pipelines.Utilities
{
    public class ExcelDataRowHelper
    {
        public static ExcelDataRowModel GetDataRowModel(DataRow dataRow, ExpenseTrackerExcelOptions config)
        {
            var transactionOutflow = dataRow[config.TransactionOutflowIndex].ParseToDecimal() ?? 0;
            var transactionInflow = dataRow[config.TransactionInflowIndex].ParseToDecimal() ?? 0;

            var model = new ExcelDataRowModel
            {
                TransactionDate = dataRow[config.TransactionDateIndex].ParseToDateTime(),
                TransactionDescription = dataRow[config.TransactionDescriptionIndex]?.ToString() ?? string.Empty,
                TransactionAmount = transactionOutflow > 0 ? transactionOutflow * -1 : transactionInflow > 0 ? transactionInflow : 0,
                TransactionAccountName = dataRow[config.TransactionAccountNameIndex]?.ToString() ?? string.Empty,
                TransferDate = dataRow[config.TransferDateIndex].ParseToDateTime(),
                TransferDescription = dataRow[config.TransferDescriptionIndex]?.ToString() ?? string.Empty,
                TransferAmount = dataRow[config.TransferAmountIndex].ParseToDecimal() ?? 0,
                TransferAccountFrom = dataRow[config.TransferAccountFromIndex]?.ToString() ?? string.Empty,
                TransferAccountTo = dataRow[config.TransferAccountToIndex]?.ToString() ?? string.Empty
            };

            return model;
        }

        public static Transaction ExtractStandardTransaction(ExcelDataRowModel model)
        {
            var transaction = new Transaction
            {
                Amount = model.TransactionAmount,
                Date = model.TransactionDate,
                Description = model.TransactionDescription,
                FinancialAccountName = model.TransactionAccountName,
                IsTransfer = false,
                CategoryDescription = string.Empty
            };

            return transaction;
        }

        public static Transaction ExtractOutgoingTransfer(ExcelDataRowModel model)
        {
            var transaction = new Transaction
            {
                Amount = model.TransferAmount * -1,
                Date = model.TransferDate,
                Description = model.TransferDescription,
                FinancialAccountName = model.TransferAccountFrom,
                IsTransfer = true
            };

            return transaction;
        }

        public static Transaction ExtractIngoingTransfer(ExcelDataRowModel model)
        {
            var transaction = new Transaction
            {
                Amount = model.TransferAmount,
                Date = model.TransferDate,
                Description = model.TransferDescription,
                FinancialAccountName = model.TransferAccountTo,
                IsTransfer = true
            };

            return transaction;
        }
    }
}
