using FluentAssertions;
using RDS.ExpenseTracker.Application.Utilities;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Tests;

public class CategoryTagMatcherTests
{
    [Fact]
    public void Match_ReturnsCategoryWhoseTagIsContainedInDescription_IgnoringCase()
    {
        var matcher = new CategoryTagMatcher(
        [
            new Category { Id = 6, Priority = 1, Tags = "esselunga, conad" },
        ]);

        matcher.Match("Pagamento carta CONAD Torino").Should().Be(6);
    }

    [Fact]
    public void Match_WhenTagsOfTwoCategoriesMatch_LowestPriorityValueWins()
    {
        var matcher = new CategoryTagMatcher(
        [
            new Category { Id = 8, Priority = 5, Tags = "amazon" },
            new Category { Id = 9, Priority = 2, Tags = "prime video" },
        ]);

        matcher.Match("AMAZON PRIME VIDEO abbonamento").Should().Be(9);
    }

    [Fact]
    public void Match_CategoryWithoutPriority_ComesAfterPrioritizedOnes()
    {
        var matcher = new CategoryTagMatcher(
        [
            new Category { Id = 1, Priority = null, Tags = "bar" },
            new Category { Id = 2, Priority = 10, Tags = "bar" },
        ]);

        matcher.Match("Bar Centrale").Should().Be(2);
    }

    [Fact]
    public void Match_SupportsSemicolonSeparatedTags_AndIgnoresCategoriesWithoutTags()
    {
        var matcher = new CategoryTagMatcher(
        [
            new Category { Id = 1, Priority = 0, Tags = "" },
            new Category { Id = 7, Priority = 3, Tags = "trenitalia;italo" },
        ]);

        matcher.Match("Biglietto Italo").Should().Be(7);
        matcher.Match("Spesa varia").Should().BeNull();
        matcher.MatchOrDefault("Spesa varia", 1).Should().Be(1);
    }
}
