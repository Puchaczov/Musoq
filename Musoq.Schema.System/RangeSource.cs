using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.System
{
    internal class RangeSource : RowSource
    {
        private readonly long _min;
        private readonly long _max;

        public RangeSource(long min, long max)
        {
            _min = min;
            _max = max;
        }

        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                for (var i = _min; i < _max; ++i)
                    yield return new EntityResolver<RangeItemEntity>(new RangeItemEntity() { Value = i }, RangeHelper.RangeToIndexMap, RangeHelper.RangeToMethodAccessMap);
            }
        }
    }
}