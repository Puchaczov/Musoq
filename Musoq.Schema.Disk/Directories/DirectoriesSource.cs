using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Disk.Directories
{
    public class DirectoriesSource : RowSourceBase<DirectoryInfo>
    {
        private readonly DirectorySourceSearchOptions _source;

        public DirectoriesSource(string path, bool recursive)
        {
            _source = new DirectorySourceSearchOptions(path, recursive);
        }

        protected override void CollectChunks(
            BlockingCollection<IReadOnlyList<EntityResolver<DirectoryInfo>>> chunkedSource)
        {
            var sources = new Stack<DirectorySourceSearchOptions>();
            sources.Push(_source);

            while (sources.Count > 0)
            {
                var source = sources.Pop();
                var dir = new DirectoryInfo(source.Path);

                var chunk = new List<EntityResolver<DirectoryInfo>>();

                foreach (var file in dir.GetDirectories())
                    chunk.Add(new EntityResolver<DirectoryInfo>(file, SchemaDirectoriesHelper.DirectoriesNameToIndexMap,
                        SchemaDirectoriesHelper.DirectoriesIndexToMethodAccessMap));

                chunkedSource.Add(chunk);

                if (!source.WithSubDirectories) continue;

                foreach (var subDir in dir.GetDirectories())
                    sources.Push(new DirectorySourceSearchOptions(subDir.FullName, source.WithSubDirectories));
            }
        }
    }
}