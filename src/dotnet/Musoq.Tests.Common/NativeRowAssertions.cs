using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        Assert.AreEqual(values.Length, visitor.VisitedCount, "The row must visit every declared column exactly once.");
        return values;
    }

    public static object ReadValue(this IReadOnlyRow row, int columnIndex)
    {
        if ((uint)columnIndex >= (uint)row.Count)
            throw new IndexOutOfRangeException();
        var visitor = new SingleAssertionVisitor(columnIndex);
        row.VisitColumns(ref visitor);
        Assert.AreEqual(1, visitor.VisitedCount, "The row must visit the selected column exactly once.");
        return visitor.Value!;
    }

    private struct SingleAssertionVisitor(int selected) : IColumnValueVisitor
    {
        public object? Value;
        public int VisitedCount;
        public void Visit<T>(int columnIndex, in T value)
        {
            if (columnIndex != selected) return;
            Value = value;
            VisitedCount++;
        }
    }

    private struct AssertionVisitor(object[] values) : IColumnValueVisitor
    {
        private readonly bool[] _visited = new bool[values.Length];
        public int VisitedCount { get; private set; }

        public void Visit<T>(int columnIndex, in T value)
        {
            Assert.IsTrue((uint)columnIndex < (uint)values.Length, "The row visited an undeclared column.");
            Assert.IsFalse(_visited[columnIndex], "The row visited a column more than once.");
            values[columnIndex] = value!;
            _visited[columnIndex] = true;
            VisitedCount++;
        }
    }
}
