using Musoq.Schema;

namespace Musoq.Tests.Common;

/// <summary>Extracts values only for existing heterogeneous test assertions.</summary>
public static class NativeRowAssertions
{
    public static object[] ReadValues(this IReadOnlyRow row)
    {
        var values = new object[row.Count];
        var visitor = new AssertionVisitor(values);
        row.VisitColumns(ref visitor);
        return values;
    }

    public static object ReadValue(this IReadOnlyRow row, int columnIndex)
    {
        if ((uint)columnIndex >= (uint)row.Count)
            throw new IndexOutOfRangeException();
        var visitor = new SingleAssertionVisitor(columnIndex);
        row.VisitColumns(ref visitor);
        return visitor.Value!;
    }

    private struct SingleAssertionVisitor(int selected) : IColumnValueVisitor
    {
        public object? Value;
        public void Visit<T>(int columnIndex, in T value)
        {
            if (columnIndex == selected) Value = value;
        }
    }

    private readonly struct AssertionVisitor(object[] values) : IColumnValueVisitor
    {
        public void Visit<T>(int columnIndex, in T value) => values[columnIndex] = value!;
    }
}
