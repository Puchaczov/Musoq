using System.Collections.Generic;
using System.IO;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Disk.Files
{
    public class FilesSource : RowSource
    {
        private readonly DirectorySourceSearchOptions _source;

        public FilesSource(string path, bool useSubDirectories)
        {
            _source = new DirectorySourceSearchOptions(path, useSubDirectories);
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

                    if (source.WithSubDirectories)
                        foreach (var subDir in dir.GetDirectories())
                            sources.Push(new DirectorySourceSearchOptions(subDir.FullName, source.WithSubDirectories));

                    foreach (var file in dir.GetFiles())
                        yield return new EntityResolver<FileInfo>(file, SchemaFilesHelper.FilesNameToIndexMap,
                            SchemaFilesHelper.FilesIndexToMethodAccessMap);
                }
            }
        }
    }
}