using RDS.ExpenseTracker.Domain.Entities;
using System.Text.RegularExpressions;

namespace RDS.ExpenseTracker.Application.Utilities;

/// <summary>
/// Normalizzazione e confronto degli IBAN: gli IBAN dei conti sono salvati senza spazi e in maiuscolo,
/// così il confronto con quelli estratti dai file di import è un semplice confronto di stringhe.
/// </summary>
public static partial class IbanUtilities
{
    [GeneratedRegex(@"^[A-Z]{2}\d{2}[A-Z0-9]{11,30}$")]
    private static partial Regex IbanFormat();

    [GeneratedRegex(@"\b[A-Z]{2}\d{2}[A-Z0-9]{11,30}\b", RegexOptions.IgnoreCase)]
    private static partial Regex IbanInText();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    public static string? Normalize(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
            return null;

        return Whitespace().Replace(iban, string.Empty).ToUpperInvariant();
    }

    public static bool IsValid(string? normalizedIban)
        => normalizedIban is not null && IbanFormat().IsMatch(normalizedIban);

    /// <summary>Tutti gli IBAN (normalizzati) presenti in un testo libero, es. la descrizione di un movimento.</summary>
    public static IEnumerable<string> ExtractAll(string? text)
        => string.IsNullOrEmpty(text)
            ? []
            : IbanInText().Matches(text).Select(m => m.Value.ToUpperInvariant()).Distinct();

    /// <summary>Il conto con l'IBAN indicato, o <c>null</c> se l'IBAN è vuoto o non associato a nessun conto.</summary>
    public static Account? FindAccount(IEnumerable<Account> accounts, string? iban)
    {
        var normalized = Normalize(iban);
        return normalized is null
            ? null
            : accounts.FirstOrDefault(a => a.Iban is not null && Normalize(a.Iban) == normalized);
    }
}
