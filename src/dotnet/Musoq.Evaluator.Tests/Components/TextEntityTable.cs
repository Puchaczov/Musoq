using System;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Table for text entities.
/// </summary>
public class TextEntityTable : ISchemaTable
{
    public ISchemaColumn[] Columns =>
    [
        new SchemaColumn(nameof(TextEntity.Name), 0, typeof(string)),
        new SchemaColumn(nameof(TextEntity.Text), 1, typeof(string)),
        new SchemaColumn(nameof(TextEntity.Line), 1, typeof(string))
    ];

    public SchemaTableMetadata Metadata => new(typeof(TextEntity));

    public ISchemaColumn GetColumnByName(string name)
    {
        return Array.Find(Columns, c => c.ColumnName == name)!;
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Array.FindAll(Columns, c => c.ColumnName == name);
    }
}
