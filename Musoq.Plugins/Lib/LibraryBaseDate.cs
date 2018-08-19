using System;
using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [BindableMethod]
        public int ExtractFromDate(string date, string partOfDate)
            => ExtractFromDate(date, CultureInfo.CurrentCulture, partOfDate);

        [BindableMethod]
        public int ExtractFromDate(string date, string culture, string partOfDate)
            => ExtractFromDate(date, new CultureInfo(culture), partOfDate);

        [BindableMethod]
        public DateTimeOffset? GetDate()
            => DateTimeOffset.Now;

        [BindableMethod]
        public DateTimeOffset? UtcGetDate()
            => DateTimeOffset.UtcNow;

        [BindableMethod]
        public int? Month(DateTimeOffset? value)
            => value?.Month;

        [BindableMethod]
        public int? Year(DateTimeOffset? value)
            => value?.Year;

        [BindableMethod]
        public int? Day(DateTimeOffset? value)
            => value?.Day;

        private static int ExtractFromDate(string date, CultureInfo culture, string partOfDate)
        {
            if (!DateTimeOffset.TryParse(date, culture, DateTimeStyles.None, out var value))
                throw new NotSupportedException($"'{date}' looks to be not valid date.");

            switch (partOfDate.ToLowerInvariant())
            {
                case "month":
                    return value.Month;
                case "year":
                    return value.Year;
                case "day":
                    return value.Day;
            }

            throw new NotSupportedException($"specified part of date value ({partOfDate}) is not valid.");
        }
    }
}
