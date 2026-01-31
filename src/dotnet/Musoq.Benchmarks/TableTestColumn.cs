using Musoq.Schema;

namespace Musoq.Benchmarks;

public class TableTestColumn : ISchemaColumn
{
    public TableTestColumn(string columnName, int columnIndex, Type columnType)
    {
        ColumnName = columnName;
        ColumnIndex = columnIndex;
        ColumnType = columnType;
    }

    public string ColumnName { get; }
    public int ColumnIndex { get; }
    public Type ColumnType { get; }
}
