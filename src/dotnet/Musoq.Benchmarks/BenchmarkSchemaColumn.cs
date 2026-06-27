using Musoq.Schema;

namespace Musoq.Benchmarks;

/// <summary>
///     Schema column for benchmarks.
/// </summary>
public class BenchmarkSchemaColumn(string columnName, int columnIndex, Type columnType) : ISchemaColumn
{
    public string ColumnName { get; } = columnName;
    public int ColumnIndex { get; } = columnIndex;
    public Type ColumnType { get; } = columnType;
}
