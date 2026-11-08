using System.Collections.Generic;

namespace Musoq.Evaluator.Tests.Schema.RuntimeV2;

public sealed class WeatherMeasurementEntity
{
    public string City { get; init; } = string.Empty;

    public double Temperature { get; init; }

    public static IReadOnlyList<WeatherMeasurementEntity> EmptyRows { get; } = [];
}
