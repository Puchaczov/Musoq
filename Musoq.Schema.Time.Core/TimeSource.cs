using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Time
{
    public class TimeSource : RowSourceBase<DateTimeOffset>
    {
        private readonly string _resolution;
        private readonly RuntimeContext _communicator;
        private readonly DateTimeOffset _startAt;
        private readonly DateTimeOffset _stopAt;

        public TimeSource(DateTimeOffset startAt, DateTimeOffset stopAt, string resolution, RuntimeContext communicator)
        {
            _startAt = startAt;
            _resolution = resolution.ToLowerInvariant();

            switch (_resolution)
            {
                case "seconds":
                    _stopAt = stopAt.Add(TimeSpan.FromMilliseconds(1));
                    break;
                case "minutes":
                    _stopAt = stopAt.AddSeconds(1);
                    break;
                case "hours":
                    _stopAt = stopAt.AddMinutes(1);
                    break;
                case "days":
                    _stopAt = stopAt.AddHours(1);
                    break;
                case "months":
                    _stopAt = stopAt.AddDays(1);
                    break;
                case "years":
                    _stopAt = stopAt.AddMonths(1);
                    break;
                default:
                    throw new NotSupportedException($"Chosen resolution '{_resolution}' is not supported.");
            }

            _communicator = communicator;
        }

        protected override void CollectChunks(
            BlockingCollection<IReadOnlyList<EntityResolver<DateTimeOffset>>> chunkedSource)
        {
            Func<DateTimeOffset, DateTimeOffset> modify;
            switch (_resolution)
            {
                case "seconds":
                    modify = offset => offset.AddSeconds(1);
                    break;
                case "minutes":
                    modify = offset => offset.AddMinutes(1);
                    break;
                case "hours":
                    modify = offset => offset.AddHours(1);
                    break;
                case "days":
                    modify = offset => offset.AddDays(1);
                    break;
                case "months":
                    modify = offset => offset.AddMonths(1);
                    break;
                case "years":
                    modify = offset => offset.AddYears(1);
                    break;
                default:
                    throw new NotSupportedException($"Chosen resolution '{_resolution}' is not supported.");
            }

            var listOfCalcTimes = new List<EntityResolver<DateTimeOffset>>();
            var currentTime = _startAt;
            var i = 0;
            var endWorkToken = _communicator.EndWorkToken;

            while (currentTime <= _stopAt)
            {
                listOfCalcTimes.Add(new EntityResolver<DateTimeOffset>(currentTime, TimeHelper.TimeNameToIndexMap,
                    TimeHelper.TimeIndexToMethodAccessMap));
                currentTime = modify(currentTime);

                if (i++ > 99)
                    continue;

                chunkedSource.Add(listOfCalcTimes, endWorkToken);
                listOfCalcTimes = new List<EntityResolver<DateTimeOffset>>();
                i = 0;
            }

            chunkedSource.Add(listOfCalcTimes, endWorkToken);
        }
    }
}