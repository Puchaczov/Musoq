using Musoq.Schema;

namespace Musoq.Benchmarks;

/// <summary>
///     Text entity table for benchmarks.
/// </summary>
public class BenchmarkTextEntityTable : ISchemaTable
{
    public ISchemaColumn[] Columns =>
    [
        new BenchmarkSchemaColumn(nameof(BenchmarkTextEntity.Name), 0, typeof(string)),
        new BenchmarkSchemaColumn(nameof(BenchmarkTextEntity.Text), 1, typeof(string))
    ];

    public SchemaTableMetadata Metadata => new(typeof(BenchmarkTextEntity));

    public ISchemaColumn GetColumnByName(string name)
    {
        return Array.Find(Columns, c => c.ColumnName == name)!;
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Array.FindAll(Columns, c => c.ColumnName == name);
    }
}
