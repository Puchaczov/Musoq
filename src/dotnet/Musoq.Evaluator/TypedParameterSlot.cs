using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace Musoq.Evaluator;

internal abstract class TypedParameterSlot
{
    public abstract Type ValueType { get; }
    public abstract bool IsNull { get; }
    public abstract TypedParameterSlot Capture(CancellationToken cancellationToken);
}

internal sealed class ScalarParameterSlot<T>(T value) : TypedParameterSlot
{
    public T Value { get; } = value;
    public override Type ValueType => typeof(T);
    public override bool IsNull => Value is null;

    public override TypedParameterSlot Capture(CancellationToken cancellationToken) => this;
}

internal sealed class CollectionParameterSlot<T>(IReadOnlyList<T>? values) : TypedParameterSlot
{
    public IReadOnlyList<T>? Values { get; } = values;
    public override Type ValueType => typeof(IReadOnlyList<T>);
    public override bool IsNull => Values is null;

    public override TypedParameterSlot Capture(CancellationToken cancellationToken)
    {
        if (Values is null)
            return this;
        var captured = new T[Values.Count];
        for (var index = 0; index < captured.Length; index++)
        {
            if ((index & 127) == 0)
                cancellationToken.ThrowIfCancellationRequested();
            captured[index] = Values[index];
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (typeof(T) == typeof(int) && captured is int[] integers)
            return new CollectionParameterSlot<int>(new CapturedInt32Collection(integers));

        return new CollectionParameterSlot<T>(new ReadOnlyCollection<T>(captured));
    }
}

internal sealed class CapturedInt32Collection(int[] values) : ReadOnlyCollection<int>(values)
{
    internal bool Matches(int value)
    {
        var integers = (int[])Items;
        return integers.Length switch
        {
            0 => false,
            1 => integers[0] == value,
            _ => integers.AsSpan().Contains(value),
        };
    }
}
