using RDS.ExpenseTracker.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Business.Pipelines.Factories
{
    public class TransactionEnrichmentCtorArgs
    {
        public IList<Category> Categories {get; set;}
        public IList<FinancialAccount> Accounts {get; set;}
        public Category DefaultCategory {get; set;}
        public DateTime DefaultDate { get; set; }

        public TransactionEnrichmentCtorArgs(IList<Category> categories, IList<FinancialAccount> accounts, Category defaultCategory, DateTime defaultDate)
        {
            Categories = categories ?? throw new ArgumentNullException(nameof(categories));
            Accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
            DefaultCategory = defaultCategory;
            DefaultDate = defaultDate;
        }
    }
}
