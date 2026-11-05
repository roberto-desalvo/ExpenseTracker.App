using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Models.DataImport;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;
using System.IO.Compression;
using System.Text;

namespace RDS.ExpenseTracker.Tests;

public class BankFileImportServiceTests
{
    private const int UserId = 42;

    private readonly FakeImporter _excel = new();
    private readonly FakeImporter _bbva = new();
    private readonly FakeImporter _sella = new();
    private readonly FakeImporter _satisPay = new();
    private readonly FakeImporter _tradeRepublic = new();
    private readonly FakeImportLogService _importLog = new();

    // un conto per ogni banca, più un conto senza tipo e uno di un tipo non importabile
    private readonly FakeAccountRepository _accounts = new([
        TestAccounts.Create(1, "BBVA", UserId),
        TestAccounts.Create(2, "Sella", UserId),
        TestAccounts.Create(3, "Satispay", UserId),
        TestAccounts.Create(4, "Trade Republic", UserId),
        TestAccounts.Create(5, "Conto senza tipo", UserId),
        TestAccounts.Create(6, "Revolut", UserId),
    ]);

    private BankFileImportService CreateService() => new(
        _excel, _bbva, _tradeRepublic, _satisPay, _sella, _importLog, _accounts, NullLogger<BankFileImportService>.Instance);

    [Theory]
    [InlineData(BankFileSource.Bbva)]
    [InlineData(BankFileSource.Sella)]
    [InlineData(BankFileSource.SatisPay)]
    [InlineData(BankFileSource.TradeRepublic)]
    public async Task ImportAsync_RoutesCsvToTheBankService(BankFileSource source)
    {
        var service = CreateService();
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("a,b\n1,2\n"));

        var result = await service.ImportAsync(source, BankFileFormat.Csv, content, "file.csv", UserId);

        result.IsSuccess.Should().BeTrue();
        var importer = ImporterFor(source);
        var call = importer.Calls.Should().ContainSingle().Subject;
        call.FileName.Should().Be("file.csv");
        call.Content.Should().Be("a,b\n1,2\n");
        call.UserId.Should().Be(UserId);
        call.AccountId.Should().Be(_accounts.Accounts.Single(a => a.AccountTypeNavigation?.ImportSource == source).Id);
        AllImporters().Except([importer]).Should().OnlyContain(i => i.Calls.Count == 0);
    }

    [Fact]
    public async Task ImportAsync_FailsAndRecordsImportLog_WhenUserHasNoAccountOfTheBankType()
    {
        _accounts.Accounts.RemoveAll(a => a.AccountTypeId == AccountType.SellaId);
        var service = CreateService();

        var result = await service.ImportAsync(BankFileSource.Sella, BankFileFormat.Csv, new MemoryStream([1]), "sella.csv", UserId);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("Sella"));
        _sella.Calls.Should().BeEmpty();
        _importLog.Records.Should().ContainSingle().Which.Errors.Should().ContainSingle(e => e.Contains("Sella"));
    }

    [Fact]
    public async Task ImportWithoutLogAsync_UsesTheGivenAccount_AndDoesNotRecordImportLog()
    {
        var service = CreateService();
        var statistics = new ImportStatistics();

        var result = await service.ImportWithoutLogAsync(
            BankFileSource.Bbva, BankFileFormat.Csv, new MemoryStream(Encoding.UTF8.GetBytes("x")), "bbva.csv", UserId,
            importAll: false, statistics, accountId: 1);

        result.IsSuccess.Should().BeTrue();
        _bbva.Calls.Should().ContainSingle().Which.AccountId.Should().Be(1);
        statistics.AccountId.Should().Be(1);
        _importLog.Records.Should().BeEmpty();
    }

    [Theory]
    [InlineData(5)] // conto senza tipo
    [InlineData(6)] // tipo non importabile
    [InlineData(2)] // conto Sella usato per un file BBVA
    public async Task ImportWithoutLogAsync_Fails_WhenTheGivenAccountIsNotOfTheBankType(int accountId)
    {
        var service = CreateService();

        var result = await service.ImportWithoutLogAsync(
            BankFileSource.Bbva, BankFileFormat.Csv, new MemoryStream([1]), "bbva.csv", UserId,
            importAll: false, new ImportStatistics(), accountId);

        result.IsFailed.Should().BeTrue();
        _bbva.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportWithoutLogAsync_Fails_WhenTheGivenAccountBelongsToAnotherUser()
    {
        var service = CreateService();

        var result = await service.ImportWithoutLogAsync(
            BankFileSource.Bbva, BankFileFormat.Csv, new MemoryStream([1]), "bbva.csv", UserId + 1,
            importAll: false, new ImportStatistics(), accountId: 1);

        result.IsFailed.Should().BeTrue();
        _bbva.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportAsync_RoutesExcelToTheExcelService_RegardlessOfFormat()
    {
        var service = CreateService();
        using var content = new MemoryStream([1, 2, 3]);

        await service.ImportAsync(BankFileSource.Excel, BankFileFormat.Xlsx, content, "movimenti.xlsx", UserId);

        _excel.Calls.Should().ContainSingle().Which.FileName.Should().Be("movimenti.xlsx");
        _importLog.Records.Should().ContainSingle().Which.Source.Should().Be("Excel");
    }

    [Theory]
    [InlineData(BankFileSource.Bbva, "Data,Importo\n01/02/2026,\"1,5\"")]
    [InlineData(BankFileSource.SatisPay, "Data;Importo\n01/02/2026;1,5")]
    public async Task ImportAsync_ConvertsBankXlsxToCsv_WithTheBankDelimiter(BankFileSource source, string expectedCsv)
    {
        var service = CreateService();
        using var xlsx = BuildXlsx(["Data", "Importo"], ["01/02/2026", "1,5"]);

        var result = await service.ImportAsync(source, BankFileFormat.Xlsx, xlsx, "estratto.xlsx", UserId);

        result.IsSuccess.Should().BeTrue();
        var call = ImporterFor(source).Calls.Should().ContainSingle().Subject;
        call.FileName.Should().Be("estratto.csv");
        call.Content.ReplaceLineEndings("\n").TrimEnd('\n').Should().Be(expectedCsv);

        // ImportLogs conserva il nome del file originale.
        _importLog.Records.Should().ContainSingle().Which.FileName.Should().Be("estratto.xlsx");
    }

    [Fact]
    public async Task ImportAsync_RecordsImportLog_WithDefaultSourceAndErrors_WhenImportFails()
    {
        _sella.Result = Result.Fail("Invalid header");
        var service = CreateService();

        var result = await service.ImportAsync(BankFileSource.Sella, BankFileFormat.Csv, new MemoryStream([1]), "sella.csv", UserId);

        result.IsFailed.Should().BeTrue();
        var record = _importLog.Records.Should().ContainSingle().Subject;
        record.Source.Should().Be("Sella CSV");
        record.FileName.Should().Be("sella.csv");
        record.UserId.Should().Be(UserId);
        record.Errors.Should().Equal("Invalid header");
    }

    [Fact]
    public async Task ImportAsync_RecordsImportLogAndRethrows_WhenImportThrows()
    {
        _bbva.Throw = new InvalidOperationException("boom");
        var service = CreateService();

        var act = () => service.ImportAsync(BankFileSource.Bbva, BankFileFormat.Csv, new MemoryStream([1]), "bbva.csv", UserId, logSource: "BBVA CSV stream");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        var record = _importLog.Records.Should().ContainSingle().Subject;
        record.Source.Should().Be("BBVA CSV stream");
        record.Errors.Should().Equal("boom");
    }

    [Fact]
    public async Task ImportAsync_RecordsFailure_WhenXlsxIsUnreadable()
    {
        var service = CreateService();

        var act = () => service.ImportAsync(BankFileSource.Bbva, BankFileFormat.Xlsx, new MemoryStream(Encoding.UTF8.GetBytes("not a workbook")), "bbva.xlsx", UserId);

        await act.Should().ThrowAsync<Exception>();
        _importLog.Records.Should().ContainSingle().Which.Source.Should().Be("BBVA XLSX");
        _bbva.Calls.Should().BeEmpty();
    }

    [Theory]
    [InlineData(BankFileSource.Bbva, BankFileFormat.Csv, "BBVA CSV")]
    [InlineData(BankFileSource.TradeRepublic, BankFileFormat.Xlsx, "Trade Republic XLSX")]
    [InlineData(BankFileSource.SatisPay, BankFileFormat.Csv, "Satispay CSV")]
    [InlineData(BankFileSource.Excel, BankFileFormat.Xlsx, "Excel")]
    public void DefaultLogSource_MatchesTheHttpLabels(BankFileSource source, BankFileFormat format, string expected)
        => IBankFileImportService.DefaultLogSource(source, format).Should().Be(expected);

    private FakeImporter ImporterFor(BankFileSource source) => source switch
    {
        BankFileSource.Bbva => _bbva,
        BankFileSource.Sella => _sella,
        BankFileSource.SatisPay => _satisPay,
        BankFileSource.TradeRepublic => _tradeRepublic,
        _ => _excel,
    };

    private IEnumerable<FakeImporter> AllImporters() => [_excel, _bbva, _sella, _satisPay, _tradeRepublic];

    /// <summary>XLSX minimale (un foglio, stringhe inline) costruito a mano: ExcelDataReader sa solo leggere.</summary>
    private static MemoryStream BuildXlsx(params string[][] rows)
    {
        static string Cell(string column, int row, string value)
            => $"<c r=\"{column}{row}\" t=\"inlineStr\"><is><t>{System.Security.SecurityElement.Escape(value)}</t></is></c>";

        var sheetRows = new StringBuilder();
        for (var r = 0; r < rows.Length; r++)
        {
            sheetRows.Append($"<row r=\"{r + 1}\">");
            for (var c = 0; c < rows[r].Length; c++)
                sheetRows.Append(Cell(((char)('A' + c)).ToString(), r + 1, rows[r][c]));
            sheetRows.Append("</row>");
        }

        var parts = new Dictionary<string, string>
        {
            ["[Content_Types].xml"] =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>",
            ["_rels/.rels"] =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>",
            ["xl/workbook.xml"] =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>",
            ["xl/_rels/workbook.xml.rels"] =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>",
            ["xl/worksheets/sheet1.xml"] =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                $"<sheetData>{sheetRows}</sheetData></worksheet>",
        };

        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, xml) in parts)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
                writer.Write(xml);
            }
        }

        stream.Position = 0;
        return stream;
    }

    private sealed class FakeImporter :
        IExcelImportService, IBbvaCsvImportService, ISellaCsvImportService, ISatisPayCsvImportService, ITradeRepublicCsvImportService
    {
        public List<(string FileName, string Content, int UserId, int? AccountId)> Calls { get; } = [];
        public Result<int> Result { get; set; } = FluentResults.Result.Ok(3);
        public Exception? Throw { get; set; }

        public Task<Result<int>> ImportFromCsvAsync(Stream fileStream, string fileName, Account account, bool importAll = false, ImportStatistics? statistics = null)
            => Import(fileStream, fileName, account.UserId, account.Id);

        public Task<Result<int>> ImportFromExcelAsync(Stream fileStream, string fileName, int userId, bool importAll = false, ImportStatistics? statistics = null)
            => Import(fileStream, fileName, userId, null);

        public Task<Result<int>> ImportFromExcelBase64Async(string base64Content, string fileName, int userId, bool importAll = false, ImportStatistics? statistics = null)
            => throw new NotSupportedException();

        private Task<Result<int>> Import(Stream fileStream, string fileName, int userId, int? accountId)
        {
            if (Throw is not null)
                throw Throw;

            using var reader = new StreamReader(fileStream, leaveOpen: true);
            Calls.Add((fileName, reader.ReadToEnd(), userId, accountId));
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeAccountRepository(IEnumerable<Account> seedAccounts) : IAccountRepository
    {
        public List<Account> Accounts { get; } = [.. seedAccounts];

        public Task<Account?> GetAccount(int id, int userId)
            => Task.FromResult(Accounts.FirstOrDefault(a => a.Id == id && a.UserId == userId));

        public Task<IEnumerable<Account>> GetAccounts(int userId)
            => Task.FromResult<IEnumerable<Account>>(Accounts.Where(a => a.UserId == userId));

        public Task<Account?> GetAccountByImportSource(int userId, BankFileSource source)
            => Task.FromResult(Accounts.FirstOrDefault(a => a.UserId == userId && a.AccountTypeNavigation?.ImportSource == source));

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task AddAccounts(IEnumerable<Account> accounts) => throw new NotSupportedException();
        public Task UpdateAccount(Account account) => throw new NotSupportedException();
        public Task DeleteAccounts(IEnumerable<int> accountIds) => throw new NotSupportedException();
        public Task<(IEnumerable<Account> Items, int TotalCount)> GetPagedAccounts(AccountQueryRequest request, int userId) => throw new NotSupportedException();
        public Task<bool> UpdateAvailability(int accountId, decimal amount, bool saveChanges) => throw new NotSupportedException();
        public Task<decimal> GetAvailability(int accountId, int userId) => throw new NotSupportedException();
        public Task CalculateAvailabilities(IEnumerable<Transaction> transactions) => throw new NotSupportedException();
    }

    private sealed class FakeImportLogService : IImportLogService
    {
        public List<(string Source, string? FileName, int UserId, List<string> Errors)> Records { get; } = [];

        public Task RecordAsync(string source, string? fileName, int userId, ImportStatistics statistics, IEnumerable<string>? errors)
        {
            Records.Add((source, fileName, userId, errors?.ToList() ?? []));
            return Task.CompletedTask;
        }
    }
}
