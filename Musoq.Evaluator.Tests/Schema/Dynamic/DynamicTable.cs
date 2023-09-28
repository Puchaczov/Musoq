using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.Dynamic;

public class DynamicTable : ISchemaTable
{
    public DynamicTable(IReadOnlyDictionary<string, Type> columns)
    {
        Columns = columns
            .Select((f, i) => (ISchemaColumn)new SchemaColumn(f.Key, i, f.Value))
            .ToArray();
    }

    public ISchemaColumn[] Columns { get; }

    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(col => col.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(col => col.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(DynamicObject));
}