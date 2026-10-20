namespace RDS.ExpenseTracker.Domain.Common;

/// <summary>
/// Describes a pair of accounts/description patterns that identify the two legs of a transfer
/// when no reliable IBAN is available to link them (e.g. Trade Republic/Sella to Satispay).
/// </summary>
public class TransferMatchRule
{
    public string AccountName1 { get; set; } = string.Empty;
    public string DescriptionPattern1 { get; set; } = string.Empty;
    public DescriptionMatchMode DescriptionMatchMode1 { get; set; } = DescriptionMatchMode.Contains;

    public string AccountName2 { get; set; } = string.Empty;
    public string DescriptionPattern2 { get; set; } = string.Empty;
    public DescriptionMatchMode DescriptionMatchMode2 { get; set; } = DescriptionMatchMode.Contains;

    public static bool MatchesDescription(string? description, string pattern, DescriptionMatchMode mode)
    {
        if (string.IsNullOrEmpty(description) || string.IsNullOrEmpty(pattern))
            return false;

        return mode == DescriptionMatchMode.StartsWith
            ? description.StartsWith(pattern, StringComparison.OrdinalIgnoreCase)
            : description.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>Il lato opposto di una regola che ha riconosciuto un movimento: conto e descrizione attesi sulla controparte.</summary>
public sealed record TransferRuleCounterpart(string AccountName, string DescriptionPattern, DescriptionMatchMode MatchMode)
{
    public bool Matches(string? description)
        => TransferMatchRule.MatchesDescription(description, DescriptionPattern, MatchMode);
}
