using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.RuntimeV2;

public sealed class RuntimeV2CastGroupingFeatureTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(RuntimeV2CastGroupingFeatureEntity.City), 0, typeof(string)),
        new SchemaColumn(nameof(RuntimeV2CastGroupingFeatureEntity.Department), 1, typeof(string)),
        new SchemaColumn(nameof(RuntimeV2CastGroupingFeatureEntity.Population), 2, typeof(string)),
        new SchemaColumn(nameof(RuntimeV2CastGroupingFeatureEntity.Amount), 3, typeof(string)),
        new SchemaColumn(nameof(RuntimeV2CastGroupingFeatureEntity.Id), 4, typeof(string)),
        new SchemaColumn(nameof(RuntimeV2CastGroupingFeatureEntity.CreatedAt), 5, typeof(string)),
        new SchemaColumn(nameof(RuntimeV2CastGroupingFeatureEntity.Quantity), 6, typeof(int))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(RuntimeV2CastGroupingFeatureEntity));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }
}
