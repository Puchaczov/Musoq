namespace Musoq.Evaluator.Runtime;

public interface IShardedRows<out T> : IKnownCountRows<T>
{
    int ShardCount { get; }
}
