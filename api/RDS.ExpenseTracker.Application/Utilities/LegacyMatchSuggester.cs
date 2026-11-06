using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Application.Utilities;

/// <summary>
/// Regole dell'abbinamento storico Excel ↔ movimenti a DB: risoluzione per nome di conto e categoria
/// e suggerimento delle coppie (stesso conto, importo entro <see cref="AmountTolerance"/>).
/// </summary>
public static class LegacyMatchSuggester
{
    public const decimal AmountTolerance = 0.02m;

    /// <summary>
    /// Conto il cui nome (o il nome del tipo di conto) corrisponde al "Fondo" Excel: prima per
    /// uguaglianza, poi per contenimento se il candidato è unico. Il confronto ignora maiuscole e spazi.
    /// </summary>
    public static int? ResolveAccount(string? excelAccount, IEnumerable<Account> accounts)
    {
        var target = Normalize(excelAccount);
        if (target.Length == 0)
            return null;

        var candidates = accounts
            .Select(a => (a.Id, Names: new[] { Normalize(a.Name), Normalize(a.AccountTypeNavigation?.Name) }.Where(n => n.Length > 0).ToArray()))
            .ToList();

        var exact = candidates.Where(c => c.Names.Contains(target)).ToList();
        if (exact.Count > 0)
            return exact[0].Id;

        var partial = candidates.Where(c => c.Names.Any(n => n.Contains(target) || target.Contains(n))).ToList();
        return partial.Count == 1 ? partial[0].Id : null;
    }

    /// <summary>
    /// Conti senza import automatico (Contanti, Hype): le loro righe Excel vanno create come nuovi movimenti.
    /// Si guarda il tipo del conto risolto, altrimenti il nome del "Fondo" Excel.
    /// </summary>
    public static bool IsManualAccount(string? excelAccount, Account? account)
    {
        if (account?.AccountTypeId is AccountType.ContantiId or AccountType.HypeId)
            return true;

        return Normalize(excelAccount) is "contanti" or "hype";
    }

    /// <summary>Categoria dell'utente con lo stesso nome della categoria Excel (ignorando maiuscole e spazi).</summary>
    public static int? ResolveCategory(string? excelCategory, IEnumerable<Category> categories)
    {
        var target = Normalize(excelCategory);
        if (target.Length == 0)
            return null;

        return categories.FirstOrDefault(c => Normalize(c.Name) == target)?.Id;
    }

    /// <summary>
    /// Coppie suggerite, al massimo una per riga e per movimento: stesso conto e differenza di importo
    /// entro la tolleranza; a parità si preferisce la data più vicina, poi l'importo più vicino. I
    /// movimenti già abbinati in precedenza (<c>LegacyMatchedAt</c>) sono esclusi.
    /// </summary>
    public static List<LegacyMatchSuggestionDto> Suggest(IEnumerable<LegacyExcelRowDto> rows, IEnumerable<LegacyDbTransactionDto> transactions)
    {
        var available = transactions.Where(t => t.LegacyMatchedAt is null).ToList();

        var candidates = rows
            .Where(r => r.AccountId is not null)
            .SelectMany(r => available
                .Where(t => t.AccountId == r.AccountId && Math.Abs(t.Amount - r.Amount) <= AmountTolerance)
                .Select(t => (
                    Row: r,
                    Transaction: t,
                    Days: t.Date is null ? double.MaxValue : Math.Abs((t.Date.Value.Date - r.Date.Date).TotalDays),
                    Delta: Math.Abs(t.Amount - r.Amount))))
            .OrderBy(c => c.Days)
            .ThenBy(c => c.Delta)
            .ThenBy(c => c.Row.RowNumber)
            .ThenBy(c => c.Transaction.Id);

        var usedRows = new HashSet<int>();
        var usedTransactions = new HashSet<int>();
        var suggestions = new List<LegacyMatchSuggestionDto>();
        foreach (var candidate in candidates)
        {
            if (usedRows.Contains(candidate.Row.RowNumber) || usedTransactions.Contains(candidate.Transaction.Id))
                continue;

            usedRows.Add(candidate.Row.RowNumber);
            usedTransactions.Add(candidate.Transaction.Id);
            suggestions.Add(new LegacyMatchSuggestionDto { RowNumber = candidate.Row.RowNumber, TransactionId = candidate.Transaction.Id });
        }

        return [.. suggestions.OrderBy(s => s.RowNumber)];
    }

    private static string Normalize(string? value)
        => value is null ? string.Empty : new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
