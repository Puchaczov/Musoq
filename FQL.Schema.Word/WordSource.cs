using System.Collections.Generic;
using System.IO;
using FQL.Schema.DataSources;

namespace FQL.Schema.Word
{
    public class WordSource : RowSource
    {
        private readonly DirectorySourceSearchOptions _source;

        public WordSource(string path, bool useSubDirectories)
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

                    foreach (var file in dir.GetFiles("*.docx"))
                        yield return new EntityResolver<FileInfo>(file, SchemaWordHelper.NameToIndexMap,
                            SchemaWordHelper.IndexToMethodMap);
                }
            }
        }

        private class DirectorySourceSearchOptions
        {
            public DirectorySourceSearchOptions(string path, bool useSubDirectories)
            {
                Path = path;
                WithSubDirectories = useSubDirectories;
            }

            public string Path { get; }

            public bool WithSubDirectories { get; }
        }
    }
}