using Musoq.Schema;

namespace Musoq.Benchmarks;

public class TableTestColumn(string columnName, int columnIndex, Type columnType) : ISchemaColumn
{
    public string ColumnName { get; } = columnName;
    public int ColumnIndex { get; } = columnIndex;
    public Type ColumnType { get; } = columnType;
}
