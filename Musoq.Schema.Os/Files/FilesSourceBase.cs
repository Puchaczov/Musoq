using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Os.Files
{
    public abstract class FilesSourceBase<TEntity> : RowSourceBase<TEntity>
    {
        private readonly InterCommunicator _communicator;
        private readonly DirectorySourceSearchOptions _source;

        public FilesSourceBase(string path, bool useSubDirectories, InterCommunicator communicator)
        {
            _communicator = communicator;
            _source = new DirectorySourceSearchOptions(path, useSubDirectories);
        }

        protected override void CollectChunks(BlockingCollection<IReadOnlyList<EntityResolver<TEntity>>> chunkedSource)
        {
            var sources = new Stack<DirectorySourceSearchOptions>();

            if(!Directory.Exists(_source.Path))
                return;

            var endWorkToken = _communicator.EndWorkToken;

            sources.Push(_source);

            while (sources.Count > 0)
            {
                var source = sources.Pop();
                var dir = new DirectoryInfo(source.Path);

                var dirFiles = new List<EntityResolver<TEntity>>();

                try
                {
                    foreach (var file in GetFiles(dir))
                        dirFiles.Add(CreateBasedOnFile(file));
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                chunkedSource.Add(dirFiles, endWorkToken);

                if (source.WithSubDirectories)
                    foreach (var subDir in dir.GetDirectories())
                        sources.Push(new DirectorySourceSearchOptions(subDir.FullName, source.WithSubDirectories));
            }
        }

        protected abstract EntityResolver<TEntity> CreateBasedOnFile(FileInfo file);

        protected virtual FileInfo[] GetFiles(DirectoryInfo directoryInfo) => directoryInfo.GetFiles();
    }
}