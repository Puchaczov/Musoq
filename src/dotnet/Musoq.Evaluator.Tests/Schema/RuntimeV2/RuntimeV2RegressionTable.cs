using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.RuntimeV2;

public sealed class RuntimeV2RegressionTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(RuntimeV2RegressionEntity.Id), 0, typeof(int)),
        new SchemaColumn(nameof(RuntimeV2RegressionEntity.Name), 1, typeof(string)),
        new SchemaColumn(nameof(RuntimeV2RegressionEntity.FirstName), 2, typeof(string)),
        new SchemaColumn(nameof(RuntimeV2RegressionEntity.LastName), 3, typeof(string)),
        new SchemaColumn(nameof(RuntimeV2RegressionEntity.Email), 4, typeof(string)),
        new SchemaColumn(nameof(RuntimeV2RegressionEntity.Value), 5, typeof(int)),
        new SchemaColumn(nameof(RuntimeV2RegressionEntity.Category), 6, typeof(string)),
        new SchemaColumn(nameof(RuntimeV2RegressionEntity.Department), 7, typeof(string)),
        new SchemaColumn(nameof(RuntimeV2RegressionEntity.Salary), 8, typeof(int)),
        new SchemaColumn(nameof(RuntimeV2RegressionEntity.Amount), 9, typeof(object))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(RuntimeV2RegressionEntity));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }
}
