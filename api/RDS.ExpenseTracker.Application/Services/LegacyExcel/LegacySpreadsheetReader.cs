using System.Globalization;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace RDS.ExpenseTracker.Application.Services.LegacyExcel;

public sealed record LegacySheetInfo(string Name, int Year, int Month);

// Pattern di una categoria Excel: "*spesa*" sulla colonna Uscite (C) o Entrate (D).
public sealed record LegacyCategoryPattern(string Pattern, bool IsIncomeColumn);

public sealed record LegacyCategory(string Name, IReadOnlyList<LegacyCategoryPattern> Patterns);

public sealed record LegacyRow(int RowNumber, DateTime Date, string Description, decimal Amount, string? AccountName, string? Category);

public sealed record LegacySheet(LegacySheetInfo Info, IReadOnlyList<LegacyCategory> Categories, IReadOnlyList<LegacyRow> Rows);

/// <summary>
/// Legge lo storico spese Excel (Spese.xlsx): un foglio per mese ("Gennaio2026", "Marzo 2022 "),
/// righe in A-E (Data, Ragione, Uscite, Entrate, Fondo) e categorie sotto "Parametri importanti"
/// (colonna G = nome, colonna H = formula con SUMIF(B:B,"*pattern*",C:C)). Le formule sono lette
/// via OpenXml perché ExcelDataReader espone solo i valori calcolati.
/// </summary>
public static class LegacySpreadsheetReader
{
    private const string CategoriesMarker = "Parametri importanti";
    private const string AdjustmentKeyword = "aggiustamento";

    private static readonly string[] MonthNames =
    [
        "gennaio", "febbraio", "marzo", "aprile", "maggio", "giugno",
        "luglio", "agosto", "settembre", "ottobre", "novembre", "dicembre",
    ];

    private static readonly Regex SheetNameRegex = new(@"^\s*(\p{L}+)\s*(\d{4})\s*$", RegexOptions.Compiled);

    private static readonly Regex SumIfTermRegex = new(
        @"(?<sign>[+-]?)\s*SUMIF\(\s*B:B\s*,\s*""\*?(?<pattern>[^""*]+)\*?""\s*,\s*(?<col>[CD]):[CD]\s*\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex CellReferenceRegex = new(@"^([A-Z]+)(\d+)$", RegexOptions.Compiled);

    public static IReadOnlyList<LegacySheetInfo> ListSheets(Stream stream)
    {
        using var document = SpreadsheetDocument.Open(stream, false);
        var workbook = document.WorkbookPart?.Workbook;
        if (workbook?.Sheets is null)
            return [];

        return workbook.Sheets.OfType<Sheet>()
            .Select(s => s.Name?.Value)
            .Where(name => name is not null)
            .Select(name => TryParseSheetName(name!, out var year, out var month) ? new LegacySheetInfo(name!, year, month) : null)
            .OfType<LegacySheetInfo>()
            .OrderBy(s => s.Year)
            .ThenBy(s => s.Month)
            .ToList();
    }

    /// <summary>Il foglio richiesto, oppure <c>null</c> se non esiste o non è un foglio mensile.</summary>
    public static LegacySheet? ReadSheet(Stream stream, string sheetName)
    {
        if (!TryParseSheetName(sheetName, out var year, out var month))
            return null;

        using var document = SpreadsheetDocument.Open(stream, false);
        var workbookPart = document.WorkbookPart;
        var sheet = workbookPart?.Workbook.Sheets?.OfType<Sheet>().FirstOrDefault(s => s.Name?.Value == sheetName);
        if (workbookPart is null || sheet?.Id?.Value is null)
            return null;

        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id.Value);
        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable?
            .Elements<SharedStringItem>().Select(i => i.InnerText).ToArray() ?? [];

        var cells = ReadCells(worksheetPart, sharedStrings);
        var categories = ReadCategories(cells);
        var rows = ReadRows(cells, categories);

        return new LegacySheet(new LegacySheetInfo(sheetName, year, month), categories, rows);
    }

    public static bool TryParseSheetName(string name, out int year, out int month)
    {
        year = 0;
        month = 0;
        var match = SheetNameRegex.Match(name);
        if (!match.Success)
            return false;

        var index = Array.IndexOf(MonthNames, match.Groups[1].Value.ToLowerInvariant());
        if (index < 0)
            return false;

        year = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        month = index + 1;
        return true;
    }

    /// <summary>
    /// Pattern sommati in una formula di categoria; i termini sottratti (es. rimborsi o righe di
    /// un'altra categoria) non identificano la categoria e vengono ignorati.
    /// </summary>
    public static IReadOnlyList<LegacyCategoryPattern> ParseCategoryFormula(string? formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
            return [];

        return SumIfTermRegex.Matches(formula)
            .Where(m => m.Groups["sign"].Value != "-")
            .Select(m => new LegacyCategoryPattern(
                m.Groups["pattern"].Value.Trim(),
                m.Groups["col"].Value.Equals("D", StringComparison.OrdinalIgnoreCase)))
            .Where(p => p.Pattern.Length > 0)
            .DistinctBy(p => (p.Pattern.ToLowerInvariant(), p.IsIncomeColumn))
            .ToList();
    }

    /// <summary>
    /// Prima categoria (nell'ordine del foglio) con un pattern contenuto nella descrizione, preferendo
    /// i pattern della colonna coerente con il segno (Uscite per importi negativi, Entrate per positivi).
    /// </summary>
    public static string? AssignCategory(IReadOnlyList<LegacyCategory> categories, string description, decimal amount)
    {
        var isIncome = amount > 0;
        return FindCategory(categories, description, p => p.IsIncomeColumn == isIncome)
            ?? FindCategory(categories, description, _ => true);
    }

    private static string? FindCategory(IReadOnlyList<LegacyCategory> categories, string description, Func<LegacyCategoryPattern, bool> filter)
        => categories.FirstOrDefault(c => c.Patterns.Where(filter)
            .Any(p => description.Contains(p.Pattern, StringComparison.InvariantCultureIgnoreCase)))?.Name;

    private sealed record CellData(string? Value, string? Formula);

    private static Dictionary<(string Column, int Row), CellData> ReadCells(WorksheetPart worksheetPart, string[] sharedStrings)
    {
        var cells = new Dictionary<(string, int), CellData>();
        foreach (var cell in worksheetPart.Worksheet.Descendants<Cell>())
        {
            var reference = cell.CellReference?.Value;
            if (reference is null)
                continue;

            var match = CellReferenceRegex.Match(reference);
            if (!match.Success)
                continue;

            var raw = cell.CellValue?.Text;
            string? value = raw;
            if (cell.DataType?.Value == CellValues.SharedString
                && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
                && index >= 0 && index < sharedStrings.Length)
            {
                value = sharedStrings[index];
            }
            else if (cell.DataType?.Value == CellValues.InlineString)
            {
                value = cell.InlineString?.InnerText;
            }

            var formula = cell.CellFormula?.Text;
            if (string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(formula))
                continue;

            cells[(match.Groups[1].Value, int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture))] = new CellData(value, formula);
        }

        return cells;
    }

    private static List<LegacyCategory> ReadCategories(Dictionary<(string Column, int Row), CellData> cells)
    {
        var markerRow = cells
            .Where(c => c.Key.Column == "G" && c.Value.Value?.Trim().Equals(CategoriesMarker, StringComparison.OrdinalIgnoreCase) == true)
            .Select(c => (int?)c.Key.Row)
            .FirstOrDefault();
        if (markerRow is null)
            return [];

        var categories = new List<LegacyCategory>();
        foreach (var (key, cell) in cells.Where(c => c.Key.Column == "G" && c.Key.Row > markerRow).OrderBy(c => c.Key.Row))
        {
            var name = cell.Value?.Trim();
            if (string.IsNullOrEmpty(name))
                continue;

            var patterns = ParseCategoryFormula(cells.GetValueOrDefault(("H", key.Row))?.Formula);
            if (patterns.Count > 0 && categories.All(c => c.Name != name))
                categories.Add(new LegacyCategory(name, patterns));
        }

        return categories;
    }

    private static List<LegacyRow> ReadRows(Dictionary<(string Column, int Row), CellData> cells, IReadOnlyList<LegacyCategory> categories)
    {
        var rows = new List<LegacyRow>();
        var rowNumbers = cells.Keys.Where(k => k.Row > 1 && k.Column is "A" or "B" or "C" or "D" or "E")
            .Select(k => k.Row).Distinct().Order();

        foreach (var rowNumber in rowNumbers)
        {
            var description = cells.GetValueOrDefault(("B", rowNumber))?.Value?.Trim();
            var date = ParseDate(cells.GetValueOrDefault(("A", rowNumber))?.Value);
            var expense = ParseDecimal(cells.GetValueOrDefault(("C", rowNumber))?.Value);
            var income = ParseDecimal(cells.GetValueOrDefault(("D", rowNumber))?.Value);

            if (string.IsNullOrEmpty(description) || date is null || (expense is null && income is null))
                continue;

            if (description.Contains(AdjustmentKeyword, StringComparison.InvariantCultureIgnoreCase))
                continue;

            var amount = Math.Round((income ?? 0m) - (expense ?? 0m), 2, MidpointRounding.AwayFromZero);
            if (amount == 0m)
                continue;

            var account = cells.GetValueOrDefault(("E", rowNumber))?.Value?.Trim();
            rows.Add(new LegacyRow(
                rowNumber,
                date.Value,
                description,
                amount,
                string.IsNullOrEmpty(account) ? null : account,
                AssignCategory(categories, description, amount)));
        }

        return rows;
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial)
            && serial is > 0 and < 2958466)
        {
            return DateTime.FromOADate(serial).Date;
        }

        return DateTime.TryParseExact(value.Trim(), ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"], CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var parsed)
            ? parsed.Date
            : null;
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? Math.Round((decimal)number, 2, MidpointRounding.AwayFromZero)
            : null;
    }
}
