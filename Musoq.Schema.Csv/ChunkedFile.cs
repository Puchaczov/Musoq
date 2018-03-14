using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Csv
{
    public class ChunkedFile : IEnumerable<IObjectResolver>
    {
        private readonly BlockingCollection<List<EntityResolver<string[]>>> _readedRows;
        private readonly CancellationToken _token;

        public ChunkedFile(BlockingCollection<List<EntityResolver<string[]>>> readedRows, CancellationToken token)
        {
            _readedRows = readedRows;
            _token = token;
        }

        public IEnumerator<IObjectResolver> GetEnumerator()
        {
            return new ChunkEnumerator(_readedRows, _token);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}