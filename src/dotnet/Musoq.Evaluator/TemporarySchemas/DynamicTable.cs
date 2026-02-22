using System;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.TemporarySchemas;

public class DynamicTable : ISchemaTable
{
    public DynamicTable(ISchemaColumn[] columns, Type metadata = null)
    {
        // Group by ColumnName, ColumnIndex, ColumnType to deduplicate
        // Take the first IntendedTypeName from each group (they should all be the same for a given column)
        var distinctColumnsGroups = columns.GroupBy(f => new { f.ColumnName, f.ColumnIndex, f.ColumnType });

        Columns = distinctColumnsGroups.Select(group => new SchemaColumn(
            group.Key.ColumnName,
            group.Key.ColumnIndex,
            group.Key.ColumnType,
            group.First().IntendedTypeName)
        ).Cast<ISchemaColumn>().ToArray();
        Metadata = new SchemaTableMetadata(metadata ?? typeof(object));
    }

    public ISchemaColumn[] Columns { get; }

    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column =>
            string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column =>
            string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    public SchemaTableMetadata Metadata { get; }
}
