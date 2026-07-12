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
}
