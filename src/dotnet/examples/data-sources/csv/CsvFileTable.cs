using Musoq.Schema;

namespace Musoq.Examples.DataSources.Csv;

public sealed class CsvFileTable : ISchemaTable
{
    public CsvFileTable()
        : this(Array.Empty<ISchemaColumn>())
    {
    }

    public CsvFileTable(string path)
        : this(Array.Empty<ISchemaColumn>())
    {
        _ = path;
    }

    public CsvFileTable(string path, bool hasHeader)
        : this(Array.Empty<ISchemaColumn>())
    {
        _ = path;
        _ = hasHeader;
    }

    public CsvFileTable(string path, bool hasHeader, int skipRows)
        : this(Array.Empty<ISchemaColumn>())
    {
        _ = path;
        _ = hasHeader;
        _ = skipRows;
    }

    public CsvFileTable(string path, bool hasHeader, int skipRows, string delimiter)
        : this(Array.Empty<ISchemaColumn>())
    {
        _ = path;
        _ = hasHeader;
        _ = skipRows;
        _ = delimiter;
    }

    internal CsvFileTable(ISchemaColumn[] columns)
        : this(columns, typeof(CsvRow))
    {
    }

    internal CsvFileTable(ISchemaColumn[] columns, Type rowType)
    {
        Columns = columns ?? throw new ArgumentNullException(nameof(columns));
        Metadata = new SchemaTableMetadata(rowType ?? throw new ArgumentNullException(nameof(rowType)));
    }

    public ISchemaColumn[] Columns { get; }

    public SchemaTableMetadata Metadata { get; }

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
