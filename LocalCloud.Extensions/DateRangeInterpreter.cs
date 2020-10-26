using System;
using System.Collections.Generic;
using System.Linq;

namespace LocalCloud.Extensions
{
    public static class DateRangeInterpreter
    {
        public const int Unit = 1;

        public static DateRange LessRange() => GetRanges().First();
        public static DateRange GreatestRange() => GetRanges().Last();
        public static IEnumerable<DateRange> GetRanges() => 
            Enum.GetValues(typeof(DateRange)).Cast<DateRange>().OrderBy(x => x);

        public static TimeSpan GetTimeSpan(this DateRange dateRange)
        {
            var now = DateTime.Now;
            var span = dateRange switch
            {
                DateRange.Month => now.AddMonths(Unit) - now,
                DateRange.Day => now.AddDays(Unit) - now,
                _ => now.AddYears(Unit) - now,
            };
            return span;
        }

        public static int GetCurrentDateValue(this DateRange dateRange)
        {
            var now = DateTime.Now;
            var value = dateRange switch
            {
                DateRange.Month => now.Month,
                DateRange.Day => now.Day,
                _ => now.Year,
            };
            return value;
        }
    }
}
