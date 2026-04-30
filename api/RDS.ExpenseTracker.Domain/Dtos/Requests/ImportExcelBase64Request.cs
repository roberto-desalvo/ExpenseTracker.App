namespace RDS.ExpenseTracker.Domain.Dtos.Requests;

public class ImportExcelBase64Request
{
    public string Base64Content { get; set; } = string.Empty;
    public string FileName { get; set; } = "import.xlsx";
}
