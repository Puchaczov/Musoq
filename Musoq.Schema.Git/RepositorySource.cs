using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using LibGit2Sharp;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Git
{
    public abstract class RepositorySource<TItem> : RowSourceBase<TItem>, IDisposable
    {
        private readonly Repository _repo;
        private readonly IDictionary<string, int> _nameToIndexMap;
        private readonly IDictionary<int, Func<TItem, object>> _indexToObjectAccessMap;

        protected RepositorySource(string repo, IDictionary<string, int> nameToIndexMap, IDictionary<int, Func<TItem, object>> indexToObjectAccessMap)
        {
            _nameToIndexMap = nameToIndexMap;
            _indexToObjectAccessMap = indexToObjectAccessMap;
            _repo = new Repository(repo);
        }

        protected abstract int ChunkSize { get; }

        protected override void CollectChunks(BlockingCollection<IReadOnlyList<EntityResolver<TItem>>> chunkedSource)
        {
            var repository = _repo;
            var chunk = new List<EntityResolver<TItem>>();

            var i = 0;
            foreach (var item in GetItems(repository))
            {
                chunk.Add(new EntityResolver<TItem>(item, _nameToIndexMap, _indexToObjectAccessMap));

                if(i++ < ChunkSize)
                    continue;

                chunkedSource.Add(chunk);
                chunk = new List<EntityResolver<TItem>>();
            }

            chunkedSource.Add(chunk);
        }

        protected abstract IEnumerable<TItem> GetItems(Repository repo);

        private void ReleaseUnmanagedResources()
        {
            _repo.Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~RepositorySource()
        {
            Dispose(false);
        }
    }
}