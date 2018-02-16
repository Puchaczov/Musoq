using System.Collections.Generic;
using System.IO;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Disk.Directories
{
    public class DirectoriesSource : RowSource
    {
        private readonly DirectorySourceSearchOptions _source;

        public DirectoriesSource(string path, bool recursive)
        {
            _source = new DirectorySourceSearchOptions(path, recursive);
        }

        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                var sources = new Stack<DirectorySourceSearchOptions>();
                sources.Push(_source);

                while (sources.Count > 0)
                {
                    var source = sources.Pop();
                    var dir = new DirectoryInfo(source.Path);

                    foreach (var file in dir.GetDirectories())
                        yield return new EntityResolver<DirectoryInfo>(file, SchemaDirectoriesHelper.DirectoriesNameToIndexMap, SchemaDirectoriesHelper.DirectoriesIndexToMethodAccessMap);

                    if (!source.WithSubDirectories) continue;

                    foreach (var subDir in dir.GetDirectories())
                        sources.Push(new DirectorySourceSearchOptions(subDir.FullName, source.WithSubDirectories));
                }
            }
        }
    }
}