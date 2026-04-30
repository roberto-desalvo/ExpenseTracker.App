namespace RDS.ExpenseTracker.Domain.Dtos;

public class ImportExcelBase64Request
{
    public string Base64Content { get; set; } = string.Empty;
    public string FileName { get; set; } = "import.xlsx";
}
