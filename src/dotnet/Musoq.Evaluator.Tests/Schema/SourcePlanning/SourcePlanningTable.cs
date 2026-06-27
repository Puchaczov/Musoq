using System;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.SourcePlanning;

public sealed class SourcePlanningTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(SourcePlanningEntity.Id), 0, typeof(int)),
        new SchemaColumn(nameof(SourcePlanningEntity.Name), 1, typeof(string)),
        new SchemaColumn(nameof(SourcePlanningEntity.Category), 2, typeof(string)),
        new SchemaColumn(nameof(SourcePlanningEntity.Score), 3, typeof(int)),
        new SchemaColumn(nameof(SourcePlanningEntity.CreatedAt), 4, typeof(DateTime)),
        new SchemaColumn(nameof(SourcePlanningEntity.JoinKey), 5, typeof(int)),
        new SchemaColumn(nameof(SourcePlanningEntity.ExpensivePayload), 6, typeof(string))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(SourcePlanningEntity));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }
}
