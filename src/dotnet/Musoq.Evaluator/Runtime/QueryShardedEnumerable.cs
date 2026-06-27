using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Runtime;

public sealed class QueryShardedEnumerable<T> : IShardedRows<T>
{
    private readonly ValueShard<T>[] _shards;

    public QueryShardedEnumerable(IReadOnlyList<ValueShard<T>> shards)
    {
        ArgumentNullException.ThrowIfNull(shards);
        _shards = shards as ValueShard<T>[] ?? shards.ToArray();
        Count = 0;
        foreach (var shard in _shards)
            Count += shard.Count;
    }

    public int Count { get; }

    public int ShardCount => _shards.Length;

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var shard in _shards)
        {
            for (var index = 0; index < shard.Count; index++)
                yield return shard[index];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
