using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tests.Schema.Multi.Second;

public class SecondEntity : ICommonInterface
{
    public static readonly IReadOnlyDictionary<string, int> TestNameToIndexMap;
    public static readonly IReadOnlyDictionary<int, Func<SecondEntity, object>> TestIndexToObjectAccessMap;
    
    public string ZeroItem { get; set; }
    
    public string FirstItem { get; set; }
    
    static SecondEntity()
    {
        TestNameToIndexMap = new Dictionary<string, int>
        {
            {nameof(ZeroItem), 0},
            {nameof(FirstItem), 1}
        };
        
        TestIndexToObjectAccessMap = new Dictionary<int, Func<SecondEntity, object>>
        {
            {0, entity => entity.ZeroItem},
            {1, entity => entity.FirstItem}
        };
    }
}
