using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Musoq.Schema;

namespace Musoq.Evaluator.Tables;

internal static class TypedRowEquality
{
    internal static bool Equals(Row left, Row right)
    {
        if (left.Count != right.Count)
            return false;
        var visitor = new EqualityVisitor(right);
        left.VisitColumns(ref visitor);
        return visitor.Equal;
    }

    internal static int Hash(Row row)
    {
        var visitor = new HashVisitor();
        row.VisitColumns(ref visitor);
        return visitor.Value.ToHashCode();
    }

    private struct EqualityVisitor(Row other) : IColumnValueVisitor
    {
        internal bool Equal = true;
        public void Visit<T>(int index, in T value)
        {
            if (!Equal)
                return;
            var column = new ColumnEquality<T>(value);
            other.VisitColumn(index, ref column);
            Equal = column.Equal;
        }
    }

    private struct ColumnEquality<T>(T value) : IColumnValueVisitor
    {
        internal bool Equal;
        public void Visit<TValue>(int index, in TValue other) => Equal = value is null && other is null ||
            typeof(T) == typeof(TValue) && EqualityComparer<T>.Default.Equals(value, Unsafe.As<TValue, T>(ref Unsafe.AsRef(in other)));
    }

    private struct HashVisitor : IColumnValueVisitor
    {
        internal HashCode Value;
        public void Visit<T>(int index, in T value) => Value.Add(value, EqualityComparer<T>.Default);
    }
}
