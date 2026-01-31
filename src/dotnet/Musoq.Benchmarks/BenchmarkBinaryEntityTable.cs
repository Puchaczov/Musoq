using Musoq.Schema;

namespace Musoq.Benchmarks;

/// <summary>
///     Binary entity table for benchmarks.
/// </summary>
public class BenchmarkBinaryEntityTable : ISchemaTable
{
    public ISchemaColumn[] Columns =>
    [
        new BenchmarkSchemaColumn(nameof(BenchmarkBinaryEntity.Name), 0, typeof(string)),
        new BenchmarkSchemaColumn(nameof(BenchmarkBinaryEntity.Content), 1, typeof(byte[]))
    ];

    public SchemaTableMetadata Metadata => new(typeof(BenchmarkBinaryEntity));

    public ISchemaColumn GetColumnByName(string name)
    {
        return Array.Find(Columns, c => c.ColumnName == name)!;
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Array.FindAll(Columns, c => c.ColumnName == name);
    }
}
