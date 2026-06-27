using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.RuntimeV2;

public sealed class RuntimeV2CastGroupingFeatureRowSource(IReadOnlyList<RuntimeV2CastGroupingFeatureEntity> rows)
    : RowSourceBase<RuntimeV2CastGroupingFeatureEntity>
{
    protected override void CollectChunks(IChunkWriter<RuntimeV2CastGroupingFeatureEntity> writer)
    {
        writer.Write(rows.ToArray());
    }
}
