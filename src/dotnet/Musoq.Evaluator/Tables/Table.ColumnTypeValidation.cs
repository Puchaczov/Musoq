using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tables;

public partial class Table
{
    private readonly struct ColumnTypeValidation(Dictionary<int, Column> columns) : IColumnValueVisitor
    {
        public void Visit<T>(int columnIndex, in T value)
        {
            if (!typeof(T).IsValueType && value is null)
                return;

            var expected = columns[columnIndex].ColumnType;
            if (!expected.IsAssignableFrom(typeof(T)))
                throw new NotSupportedException(
                    $"(Add) Mismatched types. {expected.Name} is not assignable from {typeof(T).Name}");
        }
    }
}
