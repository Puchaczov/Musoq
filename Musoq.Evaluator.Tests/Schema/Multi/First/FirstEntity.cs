using System;
using System.Collections.Generic;
using Musoq.Evaluator.Tests.Schema.Multi.Second;

namespace Musoq.Evaluator.Tests.Schema.Multi.First;

public class FirstEntity : ICommonInterface
{
    public static readonly IDictionary<string, int> TestNameToIndexMap;
    public static readonly IDictionary<int, Func<FirstEntity, object>> TestIndexToObjectAccessMap;
    
    public string FirstItem { get; set; }
    
    static FirstEntity()
    {
        TestNameToIndexMap = new Dictionary<string, int>
        {
            {nameof(FirstItem), 0}
        };
        
        TestIndexToObjectAccessMap = new Dictionary<int, Func<FirstEntity, object>>
        {
            {0, entity => entity.FirstItem}
        };
    }
}