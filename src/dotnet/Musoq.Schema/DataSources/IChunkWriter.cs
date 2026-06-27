using System.Collections.Generic;
using System.Threading;

namespace Musoq.Schema.DataSources;

public interface IChunkWriter<T>
{
    CancellationToken CancellationToken { get; }

    void Write(IReadOnlyList<T> rows);
}
