using System.Globalization;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace RDS.ExpenseTracker.Application.Services;

public sealed class SellaPdfTextParser
{
    private static readonly Regex DateRegex = new(@"\b\d{2}[/\.-]\d{2}(?:[/\.-]\d{2,4})?(?:\s+\d{2}:\d{2})?\b|\b\d{1,2}\s+(?:gen|feb|mar|apr|mag|giu|lug|ago|set|ott|nov|dic)[a-z]*\s+\d{2,4}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex IbanRegex = new(@"\bIT\d{2}[A-Z0-9]{11,30}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex AmountRegex = new(@"[-+]?\s*(?:€|EUR|∩┐╜)?\s*\d{1,3}(?:[\.\s]\d{3})*(?:[,\.]\d{2})(?:\s*[-+])?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ExplicitIdentifierRegex = new(@"(?:Codice\s*Identificativo|Cod\.?\s*Identificativo|ID\s*Operazione|ID)\s*[:#-]?\s*(?<id>[A-Za-z0-9\-]{4,})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex UuidRegex = new(@"\b[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}\b", RegexOptions.Compiled);
    private static readonly Regex NumericIdentifierRegex = new(@"\b\d{6,22}\b", RegexOptions.Compiled);
    private static readonly string[] NoiseLineTokens =
    [
        "La stampa di questo documento",
        "Saldo e lista movimenti",
        "al solo scopo informativo"
    ];

    public List<SellaPdfRow> Parse(Stream pdfStream)
    {
        if (pdfStream.CanSeek)
            pdfStream.Position = 0;

        using var document = PdfDocument.Open(pdfStream);

        var lines = new List<string>();
        var fullTextChunks = new List<string>();
        foreach (var page in document.GetPages())
        {
            var pageText = NormalizeWhitespace(page.Text);
            if (!string.IsNullOrWhiteSpace(pageText))
                fullTextChunks.Add(pageText);

            var pageLines = page.Text
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeWhitespace)
                .Where(static l => !string.IsNullOrWhiteSpace(l))
                .Where(static l => !IsNoiseLine(l));

            lines.AddRange(pageLines);
        }

        var blocks = BuildBlocks(lines);
        var rows = new List<SellaPdfRow>();

        foreach (var block in blocks)
        {
            var row = ParseBlock(block);
            if (row != null)
                rows.Add(row);
        }

        if (rows.Count == 0)
        {
            // Fallback: parse using sliding windows of raw lines (tolerant to broken table layout)
            rows.AddRange(ParseBySlidingWindow(lines));
        }

        if (rows.Count == 0)
        {
            // Last fallback: parse around identifier anchors from the full flattened text.
            var fullText = string.Join(" ", fullTextChunks);
            rows.AddRange(ParseByIdentifierAnchors(fullText));
        }

        // Distinct by identifier when present, otherwise by date+amount+description
        return rows
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Identifier)
                ? $"{r.Date:yyyy-MM-dd HH:mm}|{r.Amount}|{r.Description}"
                : r.Identifier,
                StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private static List<string> BuildBlocks(List<string> lines)
    {
        var blocks = new List<string>();
        var current = new List<string>();

        foreach (var line in lines)
        {
            if (DateRegex.IsMatch(line))
            {
                if (current.Count > 0)
                {
                    blocks.Add(string.Join(" ", current));
                    current.Clear();
                }
            }

            current.Add(line);
        }

        if (current.Count > 0)
            blocks.Add(string.Join(" ", current));

        return blocks;
    }

    private static IEnumerable<SellaPdfRow> ParseBySlidingWindow(List<string> lines)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var candidates = new List<string> { lines[i] };

            if (i + 1 < lines.Count)
                candidates.Add($"{lines[i]} {lines[i + 1]}");

            if (i + 2 < lines.Count)
                candidates.Add($"{lines[i]} {lines[i + 1]} {lines[i + 2]}");

            foreach (var candidate in candidates)
            {
                var row = ParseBlock(candidate);
                if (row != null)
                    yield return row;
            }
        }
    }

    private static IEnumerable<SellaPdfRow> ParseByIdentifierAnchors(string fullText)
    {
        if (string.IsNullOrWhiteSpace(fullText))
            yield break;

        var normalized = NormalizeWhitespace(fullText);

        var idMatches = UuidRegex.Matches(normalized)
            .Cast<Match>()
            .Where(m => m.Success)
            .ToList();

        // If no UUIDs are present, try explicit labels and then long numeric ids.
        if (idMatches.Count == 0)
        {
            idMatches = ExplicitIdentifierRegex.Matches(normalized)
                .Cast<Match>()
                .Where(m => m.Success)
                .ToList();
        }

        if (idMatches.Count == 0)
        {
            idMatches = NumericIdentifierRegex.Matches(normalized)
                .Cast<Match>()
                .Where(m => m.Success)
                .ToList();
        }

        foreach (var match in idMatches)
        {
            // Extract a local window around each id and try parsing it as a row.
            const int windowSize = 300;
            var start = Math.Max(0, match.Index - windowSize);
            var end = Math.Min(normalized.Length, match.Index + match.Length + windowSize);
            var window = normalized[start..end];

            var row = ParseBlock(window);
            if (row != null)
                yield return row;
        }
    }

    private static SellaPdfRow? ParseBlock(string block)
    {
        var normalized = NormalizeWhitespace(block);

        var dateMatch = DateRegex.Match(normalized);
        var amountMatch = AmountRegex.Matches(normalized).LastOrDefault();

        if (!dateMatch.Success || amountMatch == null)
            return null;

        var date = ParseDate(dateMatch.Value);
        var amount = ParseAmount(amountMatch.Value);

        if (!date.HasValue || !amount.HasValue)
            return null;

        var explicitId = ExplicitIdentifierRegex.Match(normalized);
        var identifier = explicitId.Success
            ? explicitId.Groups["id"].Value
            : UuidRegex.Match(normalized).Value;

        if (string.IsNullOrWhiteSpace(identifier))
            identifier = NumericIdentifierRegex.Match(normalized).Value;

        identifier = string.IsNullOrWhiteSpace(identifier) ? null : identifier.Trim();

        var ibanMatch = IbanRegex.Match(normalized);

        return new SellaPdfRow(
            date.Value,
            amount.Value,
            normalized,
            ibanMatch.Success ? ibanMatch.Value.ToUpperInvariant() : null,
            identifier,
            normalized);
    }

    private static DateTime? ParseDate(string value)
    {
        if (DateTime.TryParseExact(value, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var dateTime))
            return dateTime;

        if (DateTime.TryParseExact(value, "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out dateTime))
            return dateTime;

        if (DateTime.TryParseExact(value, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out dateTime))
            return dateTime;

        if (DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
            return date;

        if (DateTime.TryParseExact(value, "dd-MM-yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date))
            return date;

        if (DateTime.TryParseExact(value, "dd.MM.yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date))
            return date;

        if (DateTime.TryParseExact(value, "dd/MM/yy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date))
            return date;

        if (DateTime.TryParseExact(value, "dd-MM-yy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date))
            return date;

        if (DateTime.TryParseExact(value, "dd.MM.yy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date))
            return date;

        if (DateTime.TryParseExact(value, "dd/MM", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date))
            return new DateTime(DateTime.Today.Year, date.Month, date.Day);

        if (DateTime.TryParseExact(value, "dd-MM", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date))
            return new DateTime(DateTime.Today.Year, date.Month, date.Day);

        if (DateTime.TryParseExact(value, "dd.MM", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date))
            return new DateTime(DateTime.Today.Year, date.Month, date.Day);

        var itCulture = CultureInfo.GetCultureInfo("it-IT");

        if (DateTime.TryParseExact(value,
                ["d MMM yyyy", "dd MMM yyyy", "d MMMM yyyy", "dd MMMM yyyy", "d MMM yy", "dd MMM yy"],
                itCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out date))
            return date;

        return null;
    }

    private static decimal? ParseAmount(string value)
    {
        var normalized = value
            .Replace("€", string.Empty, StringComparison.Ordinal)
            .Replace("EUR", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("∩┐╜", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim();

        var isNegative = normalized.EndsWith("-", StringComparison.Ordinal);

        normalized = normalized.TrimEnd('+', '-');

        if (normalized.Contains(',') && normalized.Contains('.'))
            normalized = normalized.Replace(".", string.Empty, StringComparison.Ordinal);

        normalized = normalized.Replace(',', '.');

        if (!decimal.TryParse(normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var amount))
            return null;

        return isNegative ? -Math.Abs(amount) : amount;
    }

    private static string NormalizeWhitespace(string value)
        => Regex.Replace(value, @"\s+", " ").Trim();

    private static bool IsNoiseLine(string line)
    {
        if (Regex.IsMatch(line, @"^\d+\s*/\s*\d+$"))
            return true;

        return NoiseLineTokens.Any(token =>
            line.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    public sealed record SellaPdfRow(
        DateTime Date,
        decimal Amount,
        string Description,
        string? CounterpartyIban,
        string? Identifier,
        string RawText);
}
