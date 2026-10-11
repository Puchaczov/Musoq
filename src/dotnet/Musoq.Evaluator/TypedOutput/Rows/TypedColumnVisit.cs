using Musoq.Schema;

namespace Musoq.Evaluator.Tables;

/// <summary>Forwards a concrete property value to a visitor while retaining its type.</summary>
public static class TypedColumnVisit
{
    /// <summary>Visits all columns of a row with concrete per-column accessors.</summary>
    public static void All<TVisitor>(Row row, ref TVisitor visitor)
        where TVisitor : IColumnValueVisitor, allows ref struct
    {
        ArgumentNullException.ThrowIfNull(row);
        for (var index = 0; index < row.Count; index++)
            row.VisitColumn(index, ref visitor);
    }

    /// <summary>Visits a value without a boxed argument or boxed visitor.</summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static void Apply<TVisitor, TValue>(int columnIndex, TValue value, ref TVisitor visitor)
        where TVisitor : IColumnValueVisitor, allows ref struct
        => visitor.Visit(columnIndex, in value);
}
