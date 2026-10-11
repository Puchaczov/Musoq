using System.Runtime.CompilerServices;
using Musoq.Schema;

namespace Musoq.Evaluator.Tables;

public abstract partial class Row
{
    /// <summary>Visits a concrete column value without boxing the value or visitor state.</summary>
    public virtual void VisitColumn<TVisitor>(int columnIndex, ref TVisitor visitor)
        where TVisitor : IColumnValueVisitor, allows ref struct
    {
        if ((uint)columnIndex >= (uint)Count)
            throw new ArgumentOutOfRangeException(nameof(columnIndex));

        var selected = new SelectedColumnVisitor<TVisitor>(columnIndex, visitor);
        try
        {
            VisitColumns(ref selected);
        }
        finally
        {
            visitor = selected.Visitor;
        }
    }

    /// <summary>Visits all columns once, retaining each column's concrete type.</summary>
    public abstract void VisitColumns<TVisitor>(ref TVisitor visitor)
        where TVisitor : IColumnValueVisitor, allows ref struct;

    /// <summary>Reads a column with its declared CLR type.</summary>
    public virtual T GetValue<T>(int columnIndex)
    {
        var reader = new ColumnReader<T>();
        VisitColumn(columnIndex, ref reader);
        return reader.Value;
    }

    private struct ColumnReader<T> : IColumnValueVisitor
    {
        public T Value;

        public void Visit<TValue>(int columnIndex, in TValue value)
        {
            if (typeof(TValue) != typeof(T))
                throw new InvalidOperationException(
                    $"Column {columnIndex} has type '{typeof(TValue)}', not '{typeof(T)}'.");

            Value = Unsafe.As<TValue, T>(ref Unsafe.AsRef(in value));
        }
    }

    private ref struct SelectedColumnVisitor<TVisitor>(int selectedIndex, TVisitor visitor)
        : IColumnValueVisitor where TVisitor : IColumnValueVisitor, allows ref struct
    {
        private readonly int _selectedIndex = selectedIndex;
        public TVisitor Visitor = visitor;

        public void Visit<T>(int columnIndex, in T value)
        {
            if (columnIndex == _selectedIndex)
                Visitor.Visit(columnIndex, in value);
        }
    }
}
