using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("RDS.ExpenseTracker.DataImport.Business.Tests")]

namespace RDS.ExpenseTracker.DataImport.Business.Helpers
{
    public static class Utilities
    {
        internal static decimal? ParseToDecimal(this object? obj)
        {
            if (obj == null)
            {
                return null;
            }

            var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string normalizedInput = obj.ToString()?.Replace(",", decimalSeparator).Replace(".", decimalSeparator) ?? string.Empty;

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
        {
            return ContainsOne(str, false, compare);
        }

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
    }
}
