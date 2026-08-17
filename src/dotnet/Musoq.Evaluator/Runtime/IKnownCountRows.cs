namespace Musoq.Evaluator.Runtime;

public interface IKnownCountRows<out T> : IQueryRows<T>
{
    int Count { get; }
}
