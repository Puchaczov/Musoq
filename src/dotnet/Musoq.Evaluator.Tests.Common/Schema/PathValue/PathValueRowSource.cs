using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.PathValue;

public class PathValueRowSource(IEnumerable<PathValueEntity> entities) : RowSourceBase<PathValueEntity>
{
    protected override void CollectChunks(IChunkWriter<PathValueEntity> writer)
    {
        writer.Write(entities.ToList());
    }
}
