namespace RDS.ExpenseTracker.Domain.Models.DataImport;

public class TransactionsBySheetModel
{
    public string SheetName { get; set; }
    public DateTime SheetDate { get; set; }
    public IEnumerable<ImportTransactionModel> Transactions { get; set; }
    public IEnumerable<ImportTransferModel> Transfers { get; set; }

    public TransactionsBySheetModel(
        string sheetName,
        DateTime sheetDate,
        IEnumerable<ImportTransactionModel> transactions,
        IEnumerable<ImportTransferModel>? transfers = null)
    {
        SheetName = sheetName;
        SheetDate = sheetDate;
        Transactions = transactions;
        Transfers = transfers ?? [];
    }
}
