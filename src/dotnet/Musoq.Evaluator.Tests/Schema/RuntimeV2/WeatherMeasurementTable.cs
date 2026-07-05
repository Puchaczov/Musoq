using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.RuntimeV2;

public sealed class WeatherMeasurementTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(WeatherMeasurementEntity.City), 0, typeof(string)),
        new SchemaColumn(nameof(WeatherMeasurementEntity.Temperature), 1, typeof(double))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(WeatherMeasurementEntity));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }
}
