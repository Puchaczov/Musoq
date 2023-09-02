using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        var indexToNameMap = ((IDictionary<string, object>)_values.First()).ToDictionary(_ => index++, f => f.Key);
        
        chunkedSource.Add(_values.Select(a => new DynamicResolver(a, indexToNameMap)).ToList());
    }
}