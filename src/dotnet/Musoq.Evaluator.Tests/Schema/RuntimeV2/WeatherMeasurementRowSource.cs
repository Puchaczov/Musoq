using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.RuntimeV2;

public sealed class WeatherMeasurementRowSource(IReadOnlyList<WeatherMeasurementEntity> rows)
    : RowSourceBase<WeatherMeasurementEntity>
{
    protected override void CollectChunks(IChunkWriter<WeatherMeasurementEntity> writer)
    {
        writer.Write(rows.ToArray());
    }
}
