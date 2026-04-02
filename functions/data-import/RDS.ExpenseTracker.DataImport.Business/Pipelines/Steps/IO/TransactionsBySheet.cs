using RDS.ExpenseTracker.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.IO
{
    public class TransactionsBySheet
    {
        public string SheetName { get; set; }
        public DateTime SheetDate { get; set; }
        public IEnumerable<Transaction> Transactions { get; set; }

        public TransactionsBySheet(string sheetName, DateTime sheetDate, IEnumerable<Transaction> transactions)
        {
            SheetName = sheetName;
            SheetDate = sheetDate;
            Transactions = transactions;
        }
    }
}
