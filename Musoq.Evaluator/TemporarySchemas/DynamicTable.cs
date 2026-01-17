using System;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.TemporarySchemas;

public class DynamicTable : ISchemaTable
{
    public DynamicTable(ISchemaColumn[] columns, Type metadata = null)
    {
        var distinctColumnsGroups = columns.GroupBy(f => new { f.ColumnName, f.ColumnIndex, f.ColumnType });

        Columns = distinctColumnsGroups.Select(distinctColumn => new SchemaColumn(distinctColumn.Key.ColumnName,
            distinctColumn.Key.ColumnIndex, distinctColumn.Key.ColumnType)
        ).Cast<ISchemaColumn>().ToArray();
        Metadata = new SchemaTableMetadata(metadata ?? typeof(object));
    }

    public ISchemaColumn[] Columns { get; }

    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata { get; }
}