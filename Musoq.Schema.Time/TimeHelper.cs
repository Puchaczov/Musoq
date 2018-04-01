using System;
using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Time
{
    public static class TimeHelper
    {
        public static readonly IDictionary<string, int> TimeNameToIndexMap;
        public static readonly IDictionary<int, Func<DateTimeOffset, object>> TimeIndexToMethodAccessMap;
        public static readonly ISchemaColumn[] TimeColumns;

        static TimeHelper()
        {
            TimeNameToIndexMap = new Dictionary<string, int>
            {
                {"DateTime", 0},
                {nameof(DateTimeOffset.Second), 1},
                {nameof(DateTimeOffset.Minute), 2},
                {nameof(DateTimeOffset.Hour), 3},
                {nameof(DateTimeOffset.Day), 4},
                {nameof(DateTimeOffset.Month), 5},
                {nameof(DateTimeOffset.Year), 6},
                {nameof(DateTimeOffset.DayOfWeek), 7},
                {nameof(DateTimeOffset.DayOfYear), 8},
                {nameof(DateTimeOffset.TimeOfDay), 9},
            };

            TimeIndexToMethodAccessMap = new Dictionary<int, Func<DateTimeOffset, object>>
            {
                {0, date => date},
                {1, date => date.Second},
                {2, date => date.Minute},
                {3, date => date.Hour},
                {4, date => date.Day},
                {5, date => date.Month},
                {6, date => date.Year},
                {7, date => (int)date.DayOfWeek},
                {8, date => date.DayOfYear},
                {9, date => date.TimeOfDay}
            };

            TimeColumns = new ISchemaColumn[]
            {
                new SchemaColumn("DateTime", 0, typeof(DateTimeOffset)),
                new SchemaColumn(nameof(DateTimeOffset.Second), 1, typeof(int)),
                new SchemaColumn(nameof(DateTimeOffset.Minute), 2, typeof(int)),
                new SchemaColumn(nameof(DateTimeOffset.Hour), 3, typeof(int)),
                new SchemaColumn(nameof(DateTimeOffset.Day), 4, typeof(int)),
                new SchemaColumn(nameof(DateTimeOffset.Month), 5, typeof(int)),
                new SchemaColumn(nameof(DateTimeOffset.Year), 6, typeof(int)),
                new SchemaColumn(nameof(DateTimeOffset.DayOfWeek), 7, typeof(int)),
                new SchemaColumn(nameof(DateTimeOffset.DayOfYear), 8, typeof(int)),
                new SchemaColumn(nameof(DateTimeOffset.TimeOfDay), 9, typeof(TimeSpan))
            };
        }
    }
}