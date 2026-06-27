using Musoq.Schema;

namespace Musoq.Examples.DataSources.Csv;

public sealed class CsvFileTable : ISchemaTable
{
    public CsvFileTable()
        : this([])
    {
    }

    public CsvFileTable(string path)
        : this([])
    {
        _ = path;
    }

    public CsvFileTable(string path, bool hasHeader)
        : this([])
    {
        _ = path;
        _ = hasHeader;
    }

    public CsvFileTable(string path, bool hasHeader, int skipRows)
        : this([])
    {
        _ = path;
        _ = hasHeader;
        _ = skipRows;
    }

    public CsvFileTable(string path, bool hasHeader, int skipRows, string delimiter)
        : this([])
    {
        _ = path;
        _ = hasHeader;
        _ = skipRows;
        _ = delimiter;
    }

    internal CsvFileTable(ISchemaColumn[] columns)
    {
        Columns = columns ?? throw new ArgumentNullException(nameof(columns));
    }

    public ISchemaColumn[] Columns { get; }

    public SchemaTableMetadata Metadata { get; } = new(typeof(CsvRow));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column =>
            string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns
            .Where(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
