namespace RDS.ExpenseTracker.Domain.Common;

/// <summary>
/// Describes a pair of account types/description patterns that identify the two legs of a transfer
/// when no reliable IBAN is available to link them (e.g. Trade Republic/Sella to Satispay).
/// Account types are referenced by <see cref="Entities.AccountType.Name"/>; each user has at most one account per type.
/// </summary>
public class TransferMatchRule
{
    public string AccountType1 { get; set; } = string.Empty;
    public string DescriptionPattern1 { get; set; } = string.Empty;
    public DescriptionMatchMode DescriptionMatchMode1 { get; set; } = DescriptionMatchMode.Contains;

    public string AccountType2 { get; set; } = string.Empty;
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

/// <summary>Il lato opposto di una regola che ha riconosciuto un movimento: tipo di conto e descrizione attesi sulla controparte.</summary>
public sealed record TransferRuleCounterpart(string AccountType, string DescriptionPattern, DescriptionMatchMode MatchMode)
{
    public bool Matches(string? description)
        => TransferMatchRule.MatchesDescription(description, DescriptionPattern, MatchMode);
}
