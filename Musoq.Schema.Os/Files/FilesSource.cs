using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Os.Files
{
    public class FilesSource : RowSourceBase<FileInfo>
    {
        private readonly DirectorySourceSearchOptions _source;

        public FilesSource(string path, bool useSubDirectories)
        {
            _source = new DirectorySourceSearchOptions(path, useSubDirectories);
        }

        protected override void CollectChunks(BlockingCollection<IReadOnlyList<EntityResolver<FileInfo>>> chunkedSource)
        {
            var sources = new Stack<DirectorySourceSearchOptions>();

            if(!Directory.Exists(_source.Path))
                return;

            sources.Push(_source);

            while (sources.Count > 0)
            {
                var source = sources.Pop();
                var dir = new DirectoryInfo(source.Path);

                var dirFiles = new List<EntityResolver<FileInfo>>();

                try
                {
                    foreach (var file in dir.GetFiles())
                        dirFiles.Add(new EntityResolver<FileInfo>(file, SchemaFilesHelper.FilesNameToIndexMap,
                            SchemaFilesHelper.FilesIndexToMethodAccessMap));
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                chunkedSource.Add(dirFiles);

                if (source.WithSubDirectories)
                    foreach (var subDir in dir.GetDirectories())
                        sources.Push(new DirectorySourceSearchOptions(subDir.FullName, source.WithSubDirectories));
            }
        }
    }
}