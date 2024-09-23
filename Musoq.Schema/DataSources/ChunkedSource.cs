using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Musoq.Schema.DataSources;

public class ChunkedSource<T> : IEnumerable<IObjectResolver>
{
    private readonly BlockingCollection<IReadOnlyList<IObjectResolver>> _readRows;
    private readonly CancellationToken _token;

    public ChunkedSource(BlockingCollection<IReadOnlyList<IObjectResolver>> readRows, CancellationToken token)
    {
        _readRows = readRows;
        _token = token;
    }

    public IEnumerator<IObjectResolver> GetEnumerator()
    {
        return new ChunkEnumerator<T>(_readRows, _token);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}