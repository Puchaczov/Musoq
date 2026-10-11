using System.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tables;

[DebuggerDisplay("{DebugInfo()}")]
public abstract partial class Row : IEquatable<Row>, IReadOnlyRow
{
    public abstract int Count { get; }

    public virtual bool HasColumn(string name) => false;

    public virtual void AssignValue<T>(int columnNumber, T value)
    {
        throw new NotSupportedException("Column value assignment is not supported on this Row.");
    }

    public bool Equals(Row? other) => other is not null && TypedRowEquality.Equals(this, other);

    public override bool Equals(object? obj)
    {
        return Equals(obj as Row);
    }

    public override int GetHashCode() => TypedRowEquality.Hash(this);

}
