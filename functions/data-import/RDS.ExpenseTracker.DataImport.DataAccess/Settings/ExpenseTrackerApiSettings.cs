using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.DataAccess.Settings
{
    public class ExpenseTrackerApiSettings
    {
        public string CategoryEndpoint { get; set; } = string.Empty;
        public string TransactionEndpoint { get; set; } = string.Empty;
        public string AccountEndpoint { get; set; } = string.Empty;
    }
}
