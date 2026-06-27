using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Runtime;

public sealed class QueryRowShardedEnumerable<TRow> : IShardedRows<TRow>, ITableRowBatchSource<TRow>
    where TRow : Row
{
    private readonly RowShard<TRow>[] _shards;

    public QueryRowShardedEnumerable(IReadOnlyList<RowShard<TRow>> shards)
    {
        ArgumentNullException.ThrowIfNull(shards);
        _shards = shards as RowShard<TRow>[] ?? shards.ToArray();
        foreach (var shard in _shards)
            Count += shard.Count;
    }

    public int Count { get; }

    public int ShardCount => _shards.Length;

    public void AddTo(Table table)
    {
        ArgumentNullException.ThrowIfNull(table);
        table.AddDirectDeferred(_shards);
    }

    public IEnumerator<TRow> GetEnumerator()
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
