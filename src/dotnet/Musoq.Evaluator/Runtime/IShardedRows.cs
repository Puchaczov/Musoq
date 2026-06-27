using System.Collections.Generic;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Runtime;

public interface IShardedRows<out T> : IKnownCountRows<T>
{
    int ShardCount { get; }
}
