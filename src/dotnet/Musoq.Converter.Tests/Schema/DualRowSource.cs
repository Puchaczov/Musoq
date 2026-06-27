using System.Threading;
using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Converter.Tests.Schema;

public class DualRowSource : RowSourceBase<DualEntity>
{
    protected override void CollectChunks(IChunkWriter<DualEntity> writer)
    {
        writer.Write([new DualEntity()]);
    }
}
