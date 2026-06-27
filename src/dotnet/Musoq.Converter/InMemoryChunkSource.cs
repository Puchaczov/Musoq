using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Converter;

internal sealed class InMemoryChunkSource<T>(IEnumerable<IReadOnlyList<T>> chunks) : RowSource<T>
{
    public override IEnumerable<IReadOnlyList<T>> Chunks =>
        RowChunking.NormalizeSourceChunks(chunks ?? throw new ArgumentNullException(nameof(chunks)));
}
