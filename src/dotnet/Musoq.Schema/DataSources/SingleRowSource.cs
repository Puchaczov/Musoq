using System.Threading;
using System.Collections.Generic;

namespace Musoq.Schema.DataSources;

public class SingleRowSource : RowSourceBase<string>
{
    protected override void CollectChunks(IChunkWriter<string> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write([string.Empty]);
    }
}
