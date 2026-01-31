using Musoq.Schema;

namespace Musoq.Benchmarks;

/// <summary>
///     Schema column for benchmarks.
/// </summary>
public class BenchmarkSchemaColumn : ISchemaColumn
{
    public BenchmarkSchemaColumn(string columnName, int columnIndex, Type columnType)
    {
        ColumnName = columnName;
        ColumnIndex = columnIndex;
        ColumnType = columnType;
    }

    public string ColumnName { get; }
    public int ColumnIndex { get; }
    public Type ColumnType { get; }
}
