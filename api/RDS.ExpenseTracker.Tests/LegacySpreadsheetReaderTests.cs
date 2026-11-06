using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FluentAssertions;
using RDS.ExpenseTracker.Application.Services.LegacyExcel;

namespace RDS.ExpenseTracker.Tests;

public class LegacySpreadsheetReaderTests
{
    private const string SpesaFormula = "SUMIF(B:B,\"*spesa*\",C:C)";
    private const string FuoriFormula = "SUMIF(B:B,\"*fuori*\",C:C)-SUMIF(B:B,\"*fuori*\",D:D)-SUMIF(B:B,\"*bangla*\",D:D)";
    private const string TabaccoFormula = "SUMIF(B:B,\"*tabacco*\",C:C) + SUMIF(B:B,\"*siga*\",C:C)";
    private const string LavoroFormula = "SUMIF(B:B,\"*avanade*\",D:D)+ SUMIF(B:B,\"*lavoro*\",D:D)";

    [Fact]
    public void ListSheets_ReturnsOnlyMonthlySheets_OrderedByDate()
    {
        using var file = Workbook(
            ("Back-up", []),
            ("Gennaio2026", []),
            ("Marzo 2022 ", []),
            ("Novembre2022", []),
            ("Riepilogo", []));

        var sheets = LegacySpreadsheetReader.ListSheets(file);

        sheets.Should().Equal(
            new LegacySheetInfo("Marzo 2022 ", 2022, 3),
            new LegacySheetInfo("Novembre2022", 2022, 11),
            new LegacySheetInfo("Gennaio2026", 2026, 1));
    }

    [Fact]
    public void ReadSheet_ParsesRows_WithSignedAmountsAndAccount()
    {
        using var file = Workbook(("Ottobre2025", MonthSheet()));

        var sheet = LegacySpreadsheetReader.ReadSheet(file, "Ottobre2025")!;

        sheet.Info.Should().Be(new LegacySheetInfo("Ottobre2025", 2025, 10));
        sheet.Rows.Select(r => (r.RowNumber, r.Description, r.Amount, r.AccountName)).Should().Equal(
            (2, "spesa carrefour", -5.33m, "BBVA"),
            (3, "spesa tabacco", -23.8m, "BBVA"),
            (4, "bere fuori", 30m, "Satispay"),
            (5, "avanade", 1919.69m, "Sella"),
            (7, "regalo", -12m, null));
        sheet.Rows[0].Date.Should().Be(new DateTime(2025, 10, 1));
    }

    [Fact]
    public void ReadSheet_SkipsAdjustmentsAndRowsWithoutDateOrAmount()
    {
        using var file = Workbook(("Ottobre2025", MonthSheet()));

        var sheet = LegacySpreadsheetReader.ReadSheet(file, "Ottobre2025")!;

        sheet.Rows.Should().NotContain(r => r.Description.Contains("aggiustamento"));
        sheet.Rows.Should().NotContain(r => r.Description == "glovo");
    }

    [Fact]
    public void ReadSheet_ReadsCategoriesFromFormulas_IgnoringTotalsAndSubtractedTerms()
    {
        using var file = Workbook(("Ottobre2025", MonthSheet()));

        var sheet = LegacySpreadsheetReader.ReadSheet(file, "Ottobre2025")!;

        sheet.Categories.Select(c => c.Name).Should().Equal("Spesa", "Mangiare/Bere fuori", "Tabacco", "Lavoro");
        sheet.Categories[1].Patterns.Should().Equal(new LegacyCategoryPattern("fuori", false));
        sheet.Categories[2].Patterns.Select(p => p.Pattern).Should().Equal("tabacco", "siga");
    }

    [Fact]
    public void ReadSheet_AssignsFirstMatchingCategory_PreferringColumnOfTheSign()
    {
        using var file = Workbook(("Ottobre2025", MonthSheet()));

        var sheet = LegacySpreadsheetReader.ReadSheet(file, "Ottobre2025")!;

        sheet.Rows.Select(r => r.Category).Should().Equal("Spesa", "Spesa", "Mangiare/Bere fuori", "Lavoro", null);
    }

    [Fact]
    public void ReadSheet_ReturnsNull_ForUnknownSheet()
    {
        using var file = Workbook(("Ottobre2025", MonthSheet()));

        LegacySpreadsheetReader.ReadSheet(file, "Novembre2025").Should().BeNull();
    }

    [Fact]
    public void ReadSheet_ReturnsNoCategories_WhenMarkerIsMissing()
    {
        using var file = Workbook(("Maggio 2021", [("A2", 44317d, null), ("B2", "spesa", null), ("C2", 10d, null), ("E2", "Hype", null)]));

        var sheet = LegacySpreadsheetReader.ReadSheet(file, "Maggio 2021")!;

        sheet.Categories.Should().BeEmpty();
        sheet.Rows.Should().ContainSingle().Which.Category.Should().BeNull();
    }

    private static (string Ref, object Value, string? Formula)[] MonthSheet()
    {
        var oct1 = new DateTime(2025, 10, 1).ToOADate();
        return
        [
            ("A1", "Data", null), ("B1", "Ragione", null), ("C1", "Uscite", null), ("D1", "Entrate", null), ("E1", "Fondo", null),
            ("G1", "Tot uscite", null), ("G2", 100d, "SUM(C:C)-SUMIF(B:B,\"*Aggiustamento*\",C:C)"),
            ("A2", oct1, null), ("B2", "spesa carrefour", null), ("C2", 5.33d, null), ("E2", "BBVA", null),
            ("A3", oct1 + 1, null), ("B3", "spesa tabacco", null), ("C3", 23.8d, null), ("E3", "BBVA", null),
            ("A4", oct1 + 2, null), ("B4", "bere fuori", null), ("D4", 30d, null), ("E4", "Satispay ", null),
            ("A5", oct1 + 3, null), ("B5", "avanade", null), ("D5", 1919.69d, null), ("E5", "Sella", null),
            ("B6", "aggiustamento", null), ("D6", 10.01d, null), ("E6", "BBVA", null),
            ("A7", oct1 + 4, null), ("B7", "regalo", null), ("C7", 12d, null),
            ("A8", oct1 + 5, null), ("B8", "glovo", null),
            ("A9", oct1 + 6, null), ("B9", "aggiustamento saldo", null), ("C9", 1d, null), ("E9", "BBVA", null),
            ("G14", "Parametri importanti", null),
            ("G15", "Spesa", null), ("H15", 29.13d, SpesaFormula),
            ("G16", "Mangiare/Bere fuori", null), ("H16", -30d, FuoriFormula),
            ("G17", "Tabacco", null), ("H17", 23.8d, TabaccoFormula),
            ("G18", "Tot", null), ("H18", 22.93d, "SUM(H15:H17)"),
            ("G19", "Lavoro", null), ("H19", 1919.69d, LavoroFormula),
            ("G21", "Annuale", null), ("H21", 1d, "Gennaio2025!I2+Ottobre2025!I2"),
        ];
    }

    private static MemoryStream Workbook(params (string Name, (string Ref, object Value, string? Formula)[] Cells)[] sheets)
    {
        var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook(new Sheets());
            uint sheetId = 1;
            foreach (var (name, cells) in sheets)
            {
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                foreach (var group in cells.GroupBy(c => uint.Parse(new string(c.Ref.Where(char.IsDigit).ToArray()))).OrderBy(g => g.Key))
                {
                    var row = new Row { RowIndex = group.Key };
                    foreach (var (reference, value, formula) in group.OrderBy(c => c.Ref.Length).ThenBy(c => c.Ref, StringComparer.Ordinal))
                    {
                        var cell = new Cell { CellReference = reference };
                        if (formula is not null)
                            cell.CellFormula = new CellFormula(formula);
                        if (value is string text)
                        {
                            cell.DataType = CellValues.String;
                            cell.CellValue = new CellValue(text);
                        }
                        else
                        {
                            cell.CellValue = new CellValue(Convert.ToDouble(value));
                        }
                        row.Append(cell);
                    }
                    sheetData.Append(row);
                }
                worksheetPart.Worksheet = new Worksheet(sheetData);
                workbookPart.Workbook.Sheets!.Append(new Sheet
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = sheetId++,
                    Name = name,
                });
            }
            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        return stream;
    }
}
