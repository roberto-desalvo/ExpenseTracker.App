using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Business.Pipelines.Utilities
{
    public class ExcelDataRowModel
    {
        public DateTime? TransactionDate { get; set; }
        public string TransactionDescription { get; set; } = string.Empty;
        public decimal TransactionAmount { get; set; }
        public string TransactionAccountName { get; set; } = string.Empty;
        public DateTime? TransferDate { get; set; }
        public string TransferDescription { get; set; } = string.Empty;
        public decimal TransferAmount { get; set; }
        public string TransferAccountFrom { get; set; } = string.Empty;
        public string TransferAccountTo { get; set; } = string.Empty;
    }
}
