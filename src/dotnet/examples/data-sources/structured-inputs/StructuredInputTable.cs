using System.Reflection;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Examples.DataSources.StructuredInputs;

public sealed class StructuredInputTable<TRow> : ISchemaTable
{
    private static readonly ISchemaColumn[] SchemaColumns = CreateColumns();
    private static readonly IReadOnlyDictionary<string, ISchemaColumn> ColumnsByName =
        SchemaColumns.ToDictionary(static column => column.ColumnName, StringComparer.OrdinalIgnoreCase);

    public ISchemaColumn[] Columns => SchemaColumns.ToArray();

    public SchemaTableMetadata Metadata { get; } = new(typeof(TRow));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return name != null && ColumnsByName.TryGetValue(name, out var column) ? column : null;
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        var column = GetColumnByName(name);
        return column == null ? [] : [column];
    }

    private static ISchemaColumn[] CreateColumns()
    {
        return typeof(TRow)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.GetMethod != null)
            .Select((property, index) => (ISchemaColumn)new SchemaColumn(property.Name, index, property.PropertyType))
            .ToArray();
    }
}