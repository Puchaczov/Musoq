using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Os.Files
{
    public abstract class FilesSourceBase<TEntity> : RowSourceBase<TEntity>
    {
        private readonly RuntimeContext _communicator;
        private readonly DirectorySourceSearchOptions[] _source;

        protected FilesSourceBase(string path, bool useSubDirectories, RuntimeContext communicator)
        {
            _communicator = communicator;
            _source = new DirectorySourceSearchOptions[] 
            { 
                new DirectorySourceSearchOptions(new DirectoryInfo(path).FullName, useSubDirectories) 
            };
        }

        protected FilesSourceBase(IReadOnlyTable table, RuntimeContext context)
        {
            _communicator = context;
            var sources = new List<DirectorySourceSearchOptions>();

            foreach (var row in table.Rows)
            {
                sources.Add(new DirectorySourceSearchOptions(new DirectoryInfo((string)row[0]).FullName, (bool)row[1]));
            }

            _source = sources.ToArray();
        }

        protected override void CollectChunks(BlockingCollection<IReadOnlyList<IObjectResolver>> chunkedSource)
        {
            Parallel.ForEach(
                _source, 
                new ParallelOptions 
                { 
                    MaxDegreeOfParallelism = Environment.ProcessorCount 
                }, 
                (source) => 
                {
                    try
                    {

                        var sources = new Stack<DirectorySourceSearchOptions>();

                        if (!Directory.Exists(source.Path))
                            return;

                        var endWorkToken = _communicator.EndWorkToken;

                        sources.Push(source);

                        while (sources.Count > 0)
                        {
                            var currentSource = sources.Pop();
                            var dir = new DirectoryInfo(currentSource.Path);

                            var dirFiles = new List<EntityResolver<TEntity>>();

                            try
                            {
                                foreach (var file in GetFiles(dir))
                                    dirFiles.Add(CreateBasedOnFile(file, source.Path));
                            }
                            catch (UnauthorizedAccessException)
                            {
                                continue;
                            }

                            chunkedSource.Add(dirFiles, endWorkToken);

                            if (currentSource.WithSubDirectories)
                                foreach (var subDir in dir.GetDirectories())
                                    sources.Push(new DirectorySourceSearchOptions(subDir.FullName, currentSource.WithSubDirectories));
                        }
                    }
                    catch (OperationCanceledException)
                    {

                    }
                });
        }

        protected abstract EntityResolver<TEntity> CreateBasedOnFile(FileInfo file, string rootDirectory);

        protected virtual FileInfo[] GetFiles(DirectoryInfo directoryInfo) => directoryInfo.GetFiles();
    }
}