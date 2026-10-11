using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
        return new CollectionParameterSlot<T>(new ReadOnlyCollection<T>(captured));
    }
}
