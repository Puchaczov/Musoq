using System;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.PathValue;

public class PathValueSchemaTable : ISchemaTable
{
    public ISchemaColumn[] Columns =>
    [
        new SchemaColumn("Path", 0, typeof(string)),
        new SchemaColumn("Value", 1, typeof(object)),
        new SchemaColumn("Value2", 2, typeof(object))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(PathValueEntity));

    public ISchemaColumn GetColumnByName(string name)
    {
        return name switch
        {
            "Path" => Columns[0],
            "Value" => Columns[1],
            "Value2" => Columns[2],
            _ => throw new InvalidOperationException($"Column {name} does not exist.")
        };
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return [GetColumnByName(name)];
    }
}
