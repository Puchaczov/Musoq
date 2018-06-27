using System;
using System.Collections.Generic;

namespace Musoq.Schema.System
{
    public static class SystemSchemaHelper
    {
        public static readonly IDictionary<string, int> FlatNameToIndexMap;
        public static readonly IDictionary<int, Func<DualEntity, object>> FlatIndexToMethodAccessMap;

        static SystemSchemaHelper()
        {
            FlatNameToIndexMap = new Dictionary<string, int>
            {
                {nameof(DualEntity.Dummy), 0}
            };

            FlatIndexToMethodAccessMap = new Dictionary<int, Func<DualEntity, object>>
            {
                {0, info => info.Dummy}
            };
        }
    }
}
