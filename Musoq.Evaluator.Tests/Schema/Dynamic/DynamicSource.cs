using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Musoq.Plugins.Attributes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.Dynamic;

public class DynamicSource : RowSourceBase<dynamic>
{
    private readonly IEnumerable<dynamic> _values;

    public DynamicSource(IEnumerable<dynamic> values)
    {
        _values = values;
    }

    protected override void CollectChunks(BlockingCollection<IReadOnlyList<IObjectResolver>> chunkedSource)
    {
        var index = 0;
        var indexToNameMap = new Dictionary<int, string>();

        if (_values.First() is IDictionary<string, object> dict)
        {
            indexToNameMap = dict.ToDictionary(_ => index++, f => f.Key);
        }

        if (_values.First().GetType() is Type type &&
            type.GetCustomAttributes(typeof(DynamicObjectPropertyTypeHintAttribute), true).Any())
        {
            indexToNameMap = type.GetCustomAttributes(typeof(DynamicObjectPropertyTypeHintAttribute), true)
                .Cast<DynamicObjectPropertyTypeHintAttribute>()
                .ToDictionary(_ => index++, f => f.Name);
        }
        
        chunkedSource.Add(_values.Select(dynamic =>
        {
            if (dynamic is IDictionary<string, object> accessMap)
                return (IObjectResolver) new DynamicDictionaryResolver(accessMap, indexToNameMap);

            return new DynamicObjectResolver(dynamic, indexToNameMap);
        }).ToList());
    }
}
