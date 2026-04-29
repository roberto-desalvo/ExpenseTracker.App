using System.Globalization;

namespace RDS.ExpenseTracker.Application.Extensions;

public static class BasicTypeExtensions
{
    internal static decimal? ParseToDecimal(this object? obj)
    {
        if (obj == null)
        {
            return null;
        }

        var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        var normalizedInput = obj.ToString()?.Replace(",", decimalSeparator).Replace(".", decimalSeparator) ?? string.Empty;

        var parsed = decimal.TryParse(normalizedInput, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsedObj);

        return parsed ? parsedObj : null;
    }

    internal static DateTime? ParseToDateTime(this object? data)
    {
        if (data == null)
        {
            return null;
        }

        var parsed = DateTime.TryParse(data.ToString(), out var parsedData);
        return parsed ? parsedData : null;
    }

    public static bool ContainsOne(this string str, params string[] compare)
        => ContainsOne(str, false, compare);

    public static bool ContainsOne(this string str, bool ignoreCase, params string[] compare)
    {
        foreach (var s in compare)
        {
            if (str.Contains(s, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture))
            {
                return true;
            }
        }

        return false;
    }

    public static DateTime ParseDateFromSheetName(this string name)
    {
        try
        {
            var yearIndex = name.IndexOf('2');
            var year = int.Parse(name[yearIndex..].Trim());
            var monthStr = name[..yearIndex].Trim().ToLower();

            var months = new CultureInfo("it-IT").DateTimeFormat.MonthNames
                .Select(x => x.ToLowerInvariant())
                .ToArray();
            var month = Array.IndexOf(months, monthStr) + 1;

            return new DateTime(year, month, 1);
        }
        catch (Exception)
        {
            throw new FormatException($"Invalid date format in sheet name: {name}");
        }
    }
}
