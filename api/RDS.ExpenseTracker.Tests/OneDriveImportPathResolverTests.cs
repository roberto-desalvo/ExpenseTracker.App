using FluentAssertions;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Tests;

public class OneDriveImportPathResolverTests
{
    [Theory]
    [InlineData("BBVA", BankFileSource.Bbva)]
    [InlineData("bbva", BankFileSource.Bbva)]
    [InlineData("Sella", BankFileSource.Sella)]
    [InlineData("Satispay", BankFileSource.SatisPay)]
    [InlineData("Trade_Republic", BankFileSource.TradeRepublic)]
    [InlineData("Excel", BankFileSource.Excel)]
    public void ResolveBankFolder_MapsTheLogicAppFolderNames(string folderName, BankFileSource expected)
        => OneDriveImportPathResolver.ResolveBankFolder(folderName).Should().Be(expected);

    [Theory]
    [InlineData("Importati")]
    [InlineData("Errori")]
    [InlineData("Varie")]
    [InlineData("")]
    [InlineData(null)]
    public void ResolveBankFolder_ReturnsNull_ForUnknownFolders(string? folderName)
        => OneDriveImportPathResolver.ResolveBankFolder(folderName).Should().BeNull();

    [Theory]
    [InlineData(BankFileSource.Bbva, "estratto.csv", BankFileFormat.Csv)]
    [InlineData(BankFileSource.Sella, "ESTRATTO.CSV", BankFileFormat.Csv)]
    [InlineData(BankFileSource.TradeRepublic, "export.xlsx", BankFileFormat.Xlsx)]
    [InlineData(BankFileSource.Excel, "movimenti.xlsx", BankFileFormat.Xlsx)]
    public void ResolveFormat_ReturnsTheFileFormat(BankFileSource source, string fileName, BankFileFormat expected)
        => OneDriveImportPathResolver.ResolveFormat(source, fileName).Should().Be(expected);

    [Theory]
    [InlineData(BankFileSource.Bbva, "estratto.pdf")]
    [InlineData(BankFileSource.Bbva, "estratto")]
    [InlineData(BankFileSource.Bbva, "estratto.xls")]
    [InlineData(BankFileSource.Excel, "movimenti.csv")]
    public void ResolveFormat_ReturnsNull_ForUnsupportedFiles(BankFileSource source, string fileName)
        => OneDriveImportPathResolver.ResolveFormat(source, fileName).Should().BeNull();
}
