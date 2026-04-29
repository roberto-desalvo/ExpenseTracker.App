using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Api.Options;

public class ExpenseExcelFileOptions : IAppOptions, IExpenseExcelFileOptions
{
    public string SectionName => "ExpenseTrackerExcel";

    public IList<string> SheetsToIgnore { get; set; } = ["back-up", "maggio 2021", "giugno 2021", "luglio 2021", "agosto 2021"];
    public int TransactionDateIndex { get; set; } = 0;
    public int TransactionDescriptionIndex { get; set; } = 1;
    public int TransactionOutflowIndex { get; set; } = 2;
    public int TransactionInflowIndex { get; set; } = 3;
    public int TransactionAccountNameIndex { get; set; } = 4;
    public int TransferDateIndex { get; set; } = 9;
    public int TransferDescriptionIndex { get; set; } = 10;
    public int TransferAmountIndex { get; set; } = 11;
    public int TransferAccountFromIndex { get; set; } = 12;
    public int TransferAccountToIndex { get; set; } = 13;
}
