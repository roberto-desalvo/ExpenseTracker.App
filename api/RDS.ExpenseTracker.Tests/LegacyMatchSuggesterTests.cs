using FluentAssertions;
using RDS.ExpenseTracker.Application.Utilities;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Tests;

public class LegacyMatchSuggesterTests
{
    private static readonly DateTime Day = new(2025, 10, 10);

    private static LegacyExcelRowDto Row(int rowNumber, decimal amount, int? accountId = 1, int dayOffset = 0)
        => new() { RowNumber = rowNumber, Amount = amount, AccountId = accountId, Date = Day.AddDays(dayOffset), Description = $"row {rowNumber}" };

    private static LegacyDbTransactionDto Tx(int id, decimal amount, int accountId = 1, int dayOffset = 0, DateTime? matchedAt = null)
        => new() { Id = id, Amount = amount, AccountId = accountId, Date = Day.AddDays(dayOffset), LegacyMatchedAt = matchedAt };

    private static IEnumerable<(int Row, int Tx)> Pairs(IEnumerable<LegacyMatchSuggestionDto> s) => s.Select(x => (x.RowNumber, x.TransactionId));

    [Theory]
    [InlineData(-10.00, -10.02, true)]
    [InlineData(-10.00, -9.98, true)]
    [InlineData(-10.00, -10.03, false)]
    [InlineData(-10.00, 10.00, false)]
    public void Suggest_AppliesTwoCentsTolerance(double excel, double db, bool matched)
    {
        var result = LegacyMatchSuggester.Suggest([Row(2, (decimal)excel)], [Tx(1, (decimal)db)]);

        result.Should().HaveCount(matched ? 1 : 0);
    }

    [Fact]
    public void Suggest_RequiresSameAccount()
    {
        LegacyMatchSuggester.Suggest([Row(2, -5m, accountId: 1)], [Tx(1, -5m, accountId: 2)]).Should().BeEmpty();
        LegacyMatchSuggester.Suggest([Row(2, -5m, accountId: null)], [Tx(1, -5m)]).Should().BeEmpty();
    }

    [Fact]
    public void Suggest_IsOneToOne_PreferringClosestDate()
    {
        var rows = new[] { Row(2, -1.9m, dayOffset: 0), Row(3, -1.9m, dayOffset: 5) };
        var transactions = new[] { Tx(10, -1.9m, dayOffset: 6), Tx(11, -1.9m, dayOffset: 1), Tx(12, -1.9m, dayOffset: 20) };

        var result = LegacyMatchSuggester.Suggest(rows, transactions);

        Pairs(result).Should().Equal((2, 11), (3, 10));
    }

    [Fact]
    public void Suggest_SkipsTransactionsAlreadyMatched()
    {
        var result = LegacyMatchSuggester.Suggest([Row(2, -5m)], [Tx(1, -5m, matchedAt: DateTime.UtcNow), Tx(2, -5m, dayOffset: 3)]);

        Pairs(result).Should().Equal((2, 2));
    }

    [Theory]
    [InlineData("Sella", 4)]
    [InlineData("trade republic", 3)]
    [InlineData("Satispay ", 2)]
    [InlineData("Conto BBVA", 1)]
    [InlineData("PayPal", null)]
    [InlineData(null, null)]
    public void ResolveAccount_MatchesByAccountOrTypeName(string? excel, int? expected)
    {
        var accounts = new[]
        {
            TestAccounts.Create(1, "BBVA", 1),
            TestAccounts.Create(2, "Satispay", 1),
            new Account(3, "Investimenti TR", 1) { AccountTypeNavigation = TestAccounts.Types.Single(t => t.Id == AccountType.TradeRepublicId) },
            new Account(4, "Conto Sella", 1),
        };

        LegacyMatchSuggester.ResolveAccount(excel, accounts).Should().Be(expected);
    }

    [Theory]
    [InlineData("Contanti", null, true)]
    [InlineData(" hype", null, true)]
    [InlineData("Portafoglio", AccountType.ContantiId, true)]
    [InlineData("BBVA", AccountType.BbvaId, false)]
    [InlineData(null, null, false)]
    public void IsManualAccount_RecognizesCashAndHype(string? excel, int? accountTypeId, bool expected)
    {
        var account = accountTypeId is null ? null : new Account(1, "x", 1) { AccountTypeId = accountTypeId };

        LegacyMatchSuggester.IsManualAccount(excel, account).Should().Be(expected);
    }

    [Fact]
    public void ResolveCategory_MatchesByNameIgnoringCaseAndSpaces()
    {
        var categories = new[] { new Category { Id = 5, Name = "Spese casa" }, new Category { Id = 6, Name = "Svago" } };

        LegacyMatchSuggester.ResolveCategory("spese  Casa", categories).Should().Be(5);
        LegacyMatchSuggester.ResolveCategory("Tabacco", categories).Should().BeNull();
    }
}
