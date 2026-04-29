namespace RDS.ExpenseTracker.Domain.Common;

public interface IExpenseExcelFileOptions
{
    IList<string> SheetsToIgnore { get; }
    int TransactionDateIndex { get; }
    int TransactionDescriptionIndex { get; }
    int TransactionOutflowIndex { get; }
    int TransactionInflowIndex { get; }
    int TransactionAccountNameIndex { get; }
    int TransferDateIndex { get; }
    int TransferDescriptionIndex { get; }
    int TransferAmountIndex { get; }
    int TransferAccountFromIndex { get; }
    int TransferAccountToIndex { get; }
}
