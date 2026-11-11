using System;
using System.Collections.Generic;

namespace Musoq.Converter.Tests.Schema;

public static class SystemSchemaHelper
{
    public static readonly IReadOnlyDictionary<string, int> FlatNameToIndexMap = new Dictionary<string, int>
    {
        { nameof(DualEntity.Dummy), 0 }
    };

    public static readonly IReadOnlyDictionary<int, Func<DualEntity, object>> FlatIndexToMethodAccessMap =
        new Dictionary<int, Func<DualEntity, object>>
        {
            { 0, info => info.Dummy }
        };
}
