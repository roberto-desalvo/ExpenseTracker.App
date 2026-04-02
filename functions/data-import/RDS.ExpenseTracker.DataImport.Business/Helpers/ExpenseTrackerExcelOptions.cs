using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Business.Helpers
{
    public class ExpenseTrackerExcelOptions
    {
        public IList<string> SheetsToIgnore { get; set; } = new List<string>();
        public int TransactionDateIndex { get; set; }
        public int TransactionDescriptionIndex { get; set; }
        public int TransactionOutflowIndex { get; set; }
        public int TransactionInflowIndex { get; set; }
        public int TransactionAccountNameIndex { get; set; }
        public int TransferDateIndex { get; set; }
        public int TransferDescriptionIndex { get; set; }
        public int TransferAmountIndex { get; set; }
        public int TransferAccountFromIndex { get; set; }
        public int TransferAccountToIndex { get; set; }
    }
}
