using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Musoq.Schema.DataSources
{
    public class ChunkedSource<T> : IEnumerable<IObjectResolver>
    {
        private readonly BlockingCollection<IReadOnlyList<EntityResolver<T>>> _readedRows;
        private readonly CancellationToken _token;

        public ChunkedSource(BlockingCollection<IReadOnlyList<EntityResolver<T>>> readedRows, CancellationToken token)
        {
            _readedRows = readedRows;
            _token = token;
        }

        public IEnumerator<IObjectResolver> GetEnumerator()
        {
            return new ChunkEnumerator<T>(_readedRows, _token);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}