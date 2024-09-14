using System;
using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        /// <summary>
        /// Extracts part of the date from the date
        /// </summary>
        /// <param name="date">The date</param>
        /// <param name="partOfDate">Part of the date</param>
        /// <returns>Extracted part of the date</returns>
        [BindableMethod]
        public int ExtractFromDate(string? date, string partOfDate)
            => ExtractFromDate(date, CultureInfo.CurrentCulture, partOfDate);

        /// <summary>
        /// Extracts part of the date from the date based on given culture
        /// </summary>
        /// <param name="date">The date</param>
        /// <param name="culture"> The culture</param>
        /// <param name="partOfDate">Part of the date</param>
        /// <returns>Extracted part of the date</returns>
        [BindableMethod]
        public int ExtractFromDate(string date, string culture, string partOfDate)
            => ExtractFromDate(date, new CultureInfo(culture), partOfDate);

        /// <summary>
        /// Gets the current datetime
        /// </summary>
        /// <returns>Current datetime</returns>
        [BindableMethod]
        public DateTimeOffset? GetDate()
            => DateTimeOffset.Now;

        /// <summary>
        /// Gets the current datetime in UTC
        /// </summary>
        /// <returns>Current datetime in UTC</returns>
        [BindableMethod]
        public DateTimeOffset? UtcGetDate()
            => DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets the month from DateTimeOffset
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Month from a given date</returns>
        [BindableMethod]
        public int? Month(DateTimeOffset? value)
            => value?.Month;
        
        /// <summary>
        /// Gets the year from DateTimeOffset
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Year from a given date</returns>
        [BindableMethod]
        public int? Year(DateTimeOffset? value)
            => value?.Year;
        
        /// <summary>
        /// Gets the day from DateTimeOffset
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Day from a given date</returns>
        [BindableMethod]
        public int? Day(DateTimeOffset? value)
            => value?.Day;
        
        /// <summary>
        /// Gets the hour from DateTimeOffset
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Hour from a given date</returns>
        [BindableMethod]
        public int? Hour(DateTimeOffset? value)
            => value?.Hour;
        
        /// <summary>
        /// Gets the minute from DateTimeOffset
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Minute from a given date</returns>
        [BindableMethod]
        public int? Minute(DateTimeOffset? value)
            => value?.Minute;
        
        /// <summary>
        /// Gets the second from DateTimeOffset
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Second from a given date</returns>
        [BindableMethod]
        public int? Second(DateTimeOffset? value)
            => value?.Second;
        
        /// <summary>
        /// Gets the millisecond from DateTimeOffset
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Millisecond from a given date</returns>
        [BindableMethod]
        public int? Milliseconds(DateTimeOffset? value)
            => value?.Millisecond;
        
        /// <summary>
        /// Gets the day of week from DateTimeOffset
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Day of week from a given date</returns>
        [BindableMethod]
        public int? DayOfWeek(DateTimeOffset? value)
            => (int?)value?.DayOfWeek;

        /// <summary>
        /// Extracts time from DateTimeOffset
        /// </summary>
        /// <param name="dateTimeOffset">The value</param>
        /// <returns>Time from a given date</returns>
        [BindableMethod]
        public TimeSpan? ExtractTimeSpan(DateTimeOffset? dateTimeOffset)
        {
            return dateTimeOffset?.TimeOfDay;
        }

        private static int ExtractFromDate(string? date, CultureInfo culture, string partOfDate)
        {
            if (!DateTimeOffset.TryParse(date, culture, DateTimeStyles.None, out var value))
                throw new NotSupportedException($"'{date}' value looks to be not valid date.");

            return partOfDate.ToLower(culture) switch
            {
                "month" => value.Month,
                "year" => value.Year,
                "day" => value.Day,
                "hour" => value.Hour,
                "minute" => value.Minute,
                "second" => value.Second,
                "millisecond" => value.Millisecond,
                _ => throw new NotSupportedException($"specified part of date value ({partOfDate}) is not valid.")
            };
        }
    }
}
