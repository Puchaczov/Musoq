using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.EnvironmentVariable;

public class EnvironmentVariableEntityTable : ISchemaTable
{
    public EnvironmentVariableEntityTable()
    {
        Columns = new ISchemaColumn[]
        {
            new SchemaColumn(nameof(EnvironmentVariableEntity.Key), 0,
                typeof(EnvironmentVariableEntity).GetProperty(nameof(EnvironmentVariableEntity.Key)).PropertyType),
            new SchemaColumn(nameof(EnvironmentVariableEntity.Value), 1,
                typeof(EnvironmentVariableEntity).GetProperty(nameof(EnvironmentVariableEntity.Value)).PropertyType),
        };
    }

    public ISchemaColumn[] Columns { get; }

    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(col => col.ColumnName == name);
    }
}