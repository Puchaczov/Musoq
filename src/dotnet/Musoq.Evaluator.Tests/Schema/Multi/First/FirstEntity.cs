using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tests.Schema.Multi.First;

public class FirstEntity : ICommonInterface
{
    public static readonly IReadOnlyDictionary<string, int> TestNameToIndexMap;
    public static readonly IReadOnlyDictionary<int, Func<FirstEntity, object>> TestIndexToObjectAccessMap;

    static FirstEntity()
    {
        TestNameToIndexMap = new Dictionary<string, int>
        {
            { nameof(FirstItem), 0 }
        };

        TestIndexToObjectAccessMap = new Dictionary<int, Func<FirstEntity, object>>
        {
            { 0, entity => entity.FirstItem }
        };
    }

    public string FirstItem { get; set; }
}
