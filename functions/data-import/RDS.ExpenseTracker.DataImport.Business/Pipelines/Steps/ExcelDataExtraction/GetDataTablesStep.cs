using RDS.ExpenseTracker.DataImport.Business.Helpers;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.Abstractions;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Utilities;
using RDS.ExpenseTracker.DataImport.Business.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.ExcelDataExtraction
{
    public class GetDataTablesStep : IPipelineStep<DataSet, List<DataTable>>
    {
        private readonly IList<string> _sheetsToIgnore;
        private readonly ITransactionService _transactionService;
        private bool _importAll;

        public GetDataTablesStep(IList<string> sheetsToIgnore, ITransactionService transactionService, bool importAll)
        {
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _sheetsToIgnore = sheetsToIgnore;
            _importAll = importAll;
        }

        public async Task<List<DataTable>> ProcessAsync(DataSet dataSet)
        {
            return _importAll ? await GetAllDataTables(dataSet) : await GetFilteredDataTables(dataSet);
        }

        private Task<List<DataTable>> GetAllDataTables(DataSet dataSet)
        {
            var dataTables = dataSet.Tables.Cast<DataTable>();
            return Task.FromResult(dataTables.Where(dt => !dt.TableName.ToLower().ContainsOne(_sheetsToIgnore.ToArray())).ToList());
        }

        private async Task<List<DataTable>> GetFilteredDataTables(DataSet dataSet)
        {
            var results = new List<DataTable>();
            var dataTables = dataSet.Tables.Cast<DataTable>();

            var latestTransaction = await _transactionService.GetLatestAsync();

            foreach (var dt in dataTables)
            {
                if (dt.TableName.ToLower().ContainsOne(_sheetsToIgnore.ToArray()))
                {
                    continue;
                }

                var tableDate = SheetHelper.ParseDateFromSheetName(dt.TableName);
                if (latestTransaction?.Date != null && IsPreviousComparingMonthAndYear(tableDate, latestTransaction.Date.Value))
                {
                    continue;
                }

                results.Add(dt);
            }

            return results;
        }

        private static bool IsPreviousComparingMonthAndYear(DateTime item, DateTime compare)
        {
            if (item.Year < compare.Year)
            {
                return true;
            }

            if(item.Year > compare.Year)
            {
                return false;
            }

            return item.Month < compare.Month;
        }
    }


}
