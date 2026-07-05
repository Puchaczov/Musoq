using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.RuntimeV2;

public sealed class WeatherMeasurementSchemaProvider(IReadOnlyList<WeatherMeasurementEntity> rows)
    : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        return new WeatherMeasurementSchema(rows);
    }
}
