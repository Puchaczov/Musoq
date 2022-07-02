using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Os.Directories
{
    public class DirectoriesSource : RowSourceBase<DirectoryInfo>
    {
        private readonly RuntimeContext _communicator;
        private readonly DirectorySourceSearchOptions[] _sources;

        public DirectoriesSource(string path, bool recursive, RuntimeContext communicator)
        {
            _communicator = communicator;
            _sources = new DirectorySourceSearchOptions[] 
            {
                new(new DirectoryInfo(path).FullName, recursive)
            };
        }

        public DirectoriesSource(IReadOnlyTable table, RuntimeContext context)
        {
            _communicator = context;
            var sources = new List<DirectorySourceSearchOptions>();

            foreach (var row in table.Rows)
            {
                sources.Add(new DirectorySourceSearchOptions(new DirectoryInfo((string)row[0]).FullName, (bool)row[1]));
            }

            _sources = sources.ToArray();
        }

        protected override void CollectChunks(
            BlockingCollection<IReadOnlyList<IObjectResolver>> chunkedSource)
        {
            Parallel.ForEach(
                _sources,
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

                            var chunk = new List<EntityResolver<DirectoryInfo>>();

                            foreach (var file in dir.GetDirectories())
                                chunk.Add(new EntityResolver<DirectoryInfo>(file, SchemaDirectoriesHelper.DirectoriesNameToIndexMap,
                                    SchemaDirectoriesHelper.DirectoriesIndexToMethodAccessMap));

                            chunkedSource.Add(chunk, endWorkToken);

                            if (!currentSource.WithSubDirectories) continue;

                            foreach (var subDir in dir.GetDirectories())
                                sources.Push(new DirectorySourceSearchOptions(subDir.FullName, currentSource.WithSubDirectories));
                        }
                    }
                    catch (OperationCanceledException)
                    {

                    }
                });
        }
    }
}