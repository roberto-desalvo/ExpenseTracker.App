using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Business.Pipelines.Utilities
{
    public class SheetHelper
    {
        public static DateTime ParseDateFromSheetName(string name)
        {
            try
            {
                var yearIndex = name.IndexOf('2');
                var year = int.Parse(name[yearIndex..].Trim());
                var monthStr = name[..yearIndex].Trim().ToLower();

                var months = new CultureInfo("it-IT").DateTimeFormat.MonthNames.Select(x => x.ToLowerInvariant()).ToArray();
                var month = Array.IndexOf(months, monthStr) + 1;

                return new DateTime(year, month, 1);
            }
            catch (Exception)
            {
                throw new FormatException($"Invalid date format, please check date form sheet {name}");
            }
        }
    }
}
