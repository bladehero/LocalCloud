using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LocalCloud.Storage.Core
{
    public static class Extensions
    {
        public static async Task<Stream> GetStream(this string str)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            await writer.WriteLineAsync(str);
            await writer.FlushAsync();
            stream.Position = 0;
            return stream;
        }

        public static string GetPath(this DateRanges dateRange, DateTime? dateTime = null)
        {
            var source = dateTime.GetValueOrDefault(DateTime.Now);

            var path = string.Empty;
            foreach (DateRanges item in Enum.GetValues(typeof(DateRanges)))
            {
                var section = item.GetValue(source);
                path = Path.Combine(path, section);
                if (item == dateRange)
                {
                    break;
                }
            }
            return path;
        }

        public static DateRanges CastToDateRange(string source)
        {
            source = Path.Combine(source);
            var sections = source.Trim('\\').Split('\\');
            var dateRanges = Enum.GetValues(typeof(DateRanges)).Cast<DateRanges>().ToList();

            if (sections.Length > dateRanges.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(source), $"The value was provided with incorrect date format which cannot be casted to `{nameof(DateRanges)}` type!");
            }

            int index;
            for (index = 0; index < sections.Length; index++)
            {
                if (!_isValidValueForDateRange(dateRanges[index], sections[index]))
                {
                    throw new ArgumentException($"The value of `{(DateRanges)index}` couldn't be casted with a value: `{sections[index]}`", nameof(source));
                }
            }

            return (DateRanges)(index - 1);
        }

        public static string GetValue(this DateRanges dateRange,
                                      DateTime dateTime) => dateRange switch
                                      {
                                          DateRanges.Year => dateTime.Year.ToString(),
                                          DateRanges.Month => dateTime.Month.ToString(),
                                          DateRanges.Day => dateTime.Day.ToString(),
                                          _ => throw new ArgumentOutOfRangeException(nameof(dateRange), $"`{dateRange}` is not a valid value!")
                                      };

        private static bool _isValidValueForDateRange(DateRanges dateRange, string source)
        {
            if (!int.TryParse(source, out int value))
            {
                return false;
            }

            var isValid = true;
            try
            {
                switch (dateRange)
                {
                    case DateRanges.Year:
                        new DateTime(value, 1, 1);
                        break;
                    case DateRanges.Month:
                        new DateTime(1, value, 1);
                        break;
                    case DateRanges.Day:
                        new DateTime(1, 1, value);
                        break;
                    default:
                        isValid = false;
                        break;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                isValid = false;
            }
            return isValid;
        }
    }
}
