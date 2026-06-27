using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.RuntimeV2;

public sealed class RuntimeV2RegressionRowSource(IReadOnlyList<RuntimeV2RegressionEntity> rows)
    : RowSourceBase<RuntimeV2RegressionEntity>
{
    protected override void CollectChunks(IChunkWriter<RuntimeV2RegressionEntity> writer)
    {
        writer.Write(rows.ToArray());
    }
}
