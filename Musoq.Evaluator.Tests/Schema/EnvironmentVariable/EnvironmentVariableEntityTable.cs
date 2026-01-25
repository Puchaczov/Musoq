using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.EnvironmentVariable;

public class EnvironmentVariableEntityTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(EnvironmentVariableEntity.Key), 0,
            typeof(EnvironmentVariableEntity).GetProperty(nameof(EnvironmentVariableEntity.Key))!.PropertyType),
        new SchemaColumn(nameof(EnvironmentVariableEntity.Value), 1,
            typeof(EnvironmentVariableEntity).GetProperty(nameof(EnvironmentVariableEntity.Value))!.PropertyType)
    ];

    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(col => col.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(col => col.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(EnvironmentVariableEntity));
}
