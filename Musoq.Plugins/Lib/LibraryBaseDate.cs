using System;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [BindableMethod]
        public int ExtractFromDate(string date, string partOfDate)
        {
            var value = DateTimeOffset.Parse(date);

            switch (partOfDate.ToLowerInvariant())
            {
                case "month":
                    return value.Month;
                case "year":
                    return value.Year;
                case "day":
                    return value.Day;
            }

            throw new NotSupportedException();
        }

        [BindableMethod]
        public TimeSpan? DateDiff(DateTimeOffset? first, DateTimeOffset? second)
        {
            return second - first;
        }

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
    }
}
