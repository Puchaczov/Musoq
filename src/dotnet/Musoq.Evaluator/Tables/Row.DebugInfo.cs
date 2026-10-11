using System.Text;
using Musoq.Schema;

namespace Musoq.Evaluator.Tables;

public abstract partial class Row
{
    internal string DebugInfo()
    {
        if (Count == 0) return string.Empty;
        var visitor = new DebugVisitor(new StringBuilder());
        VisitColumns(ref visitor);
        return visitor.Text.ToString();
    }

    private readonly struct DebugVisitor(StringBuilder text) : IColumnValueVisitor
    {
        public StringBuilder Text { get; } = text;
        public void Visit<T>(int columnIndex, in T value)
        {
            if (columnIndex > 0) Text.Append(", ");
            Text.Append(value?.ToString());
        }
    }
}
