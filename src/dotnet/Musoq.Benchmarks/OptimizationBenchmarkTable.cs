using Musoq.Schema;

namespace Musoq.Benchmarks;

public sealed class OptimizationBenchmarkTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(OptimizationBenchmarkEntity.Id), 0, typeof(int)),
        new SchemaColumn(nameof(OptimizationBenchmarkEntity.Name), 1, typeof(string)),
        new SchemaColumn(nameof(OptimizationBenchmarkEntity.Category), 2, typeof(string)),
        new SchemaColumn(nameof(OptimizationBenchmarkEntity.GroupKey), 3, typeof(string)),
        new SchemaColumn(nameof(OptimizationBenchmarkEntity.JoinKey), 4, typeof(int)),
        new SchemaColumn(nameof(OptimizationBenchmarkEntity.Score), 5, typeof(int)),
        new SchemaColumn(nameof(OptimizationBenchmarkEntity.Value), 6, typeof(int)),
        new SchemaColumn(nameof(OptimizationBenchmarkEntity.CreatedAt), 7, typeof(DateTime)),
        new SchemaColumn(nameof(OptimizationBenchmarkEntity.Payload), 8, typeof(string))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(OptimizationBenchmarkEntity));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.FirstOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }
}
