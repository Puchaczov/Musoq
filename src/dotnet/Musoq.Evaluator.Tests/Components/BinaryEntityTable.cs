using System;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Table for binary entities.
/// </summary>
public class BinaryEntityTable : ISchemaTable
{
    public ISchemaColumn[] Columns =>
    [
        new SchemaColumn(nameof(BinaryEntity.Name), 0, typeof(string)),
        new SchemaColumn(nameof(BinaryEntity.Content), 1, typeof(byte[])),
        new SchemaColumn(nameof(BinaryEntity.Data), 1, typeof(byte[]))
    ];

    public SchemaTableMetadata Metadata => new(typeof(BinaryEntity));

    public ISchemaColumn GetColumnByName(string name)
    {
        return Array.Find(Columns, c => c.ColumnName == name)!;
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Array.FindAll(Columns, c => c.ColumnName == name);
    }
}
