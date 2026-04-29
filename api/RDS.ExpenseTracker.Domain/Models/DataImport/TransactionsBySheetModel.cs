namespace RDS.ExpenseTracker.Domain.Models.DataImport;

public class TransactionsBySheetModel
{
    public string SheetName { get; set; }
    public DateTime SheetDate { get; set; }
    public IEnumerable<ImportTransactionModel> Transactions { get; set; }

    public TransactionsBySheetModel(string sheetName, DateTime sheetDate, IEnumerable<ImportTransactionModel> transactions)
    {
        SheetName = sheetName;
        SheetDate = sheetDate;
        Transactions = transactions;
    }
}
