using System;
using System.Collections.Generic;
using System.IO;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Os.Files
{
    public static class SchemaFilesHelper
    {
        public static readonly IDictionary<string, int> FilesNameToIndexMap;
        public static readonly IDictionary<int, Func<FileInfo, object>> FilesIndexToMethodAccessMap;
        public static readonly ISchemaColumn[] FilesColumns;

        static SchemaFilesHelper()
        {
            FilesNameToIndexMap = new Dictionary<string, int>
            {
                {nameof(FileInfo.Name), 0},
                {nameof(FileInfo.CreationTime), 1},
                {nameof(FileInfo.CreationTimeUtc), 2},
                {nameof(FileInfo.DirectoryName), 3},
                {nameof(FileInfo.Extension), 4},
                {nameof(FileInfo.FullName), 5},
                {nameof(FileInfo.Exists), 6},
                {nameof(FileInfo.IsReadOnly), 7},
                {nameof(FileInfo.Length), 8},
                {"Content", 9},
                {nameof(FileInfo), 10}
            };

            FilesIndexToMethodAccessMap = new Dictionary<int, Func<FileInfo, object>>
            {
                {0, info => info.Name},
                {1, info => info.CreationTime},
                {2, info => info.CreationTimeUtc},
                {3, info => info.DirectoryName},
                {4, info => info.Extension},
                {5, info => info.FullName},
                {6, info => info.Exists},
                {7, info => info.IsReadOnly},
                {8, info => info.Length},
                {9, info => GetFileContent(info)},
                {10, info => info}
            };

            FilesColumns = new ISchemaColumn[]
            {
                new SchemaColumn(nameof(FileInfo.Name), 0, typeof(string)),
                new SchemaColumn(nameof(FileInfo.CreationTime), 1, typeof(DateTime)),
                new SchemaColumn(nameof(FileInfo.CreationTimeUtc), 2, typeof(DateTime)),
                new SchemaColumn(nameof(FileInfo.DirectoryName), 3, typeof(string)),
                new SchemaColumn(nameof(FileInfo.Extension), 4, typeof(string)),
                new SchemaColumn(nameof(FileInfo.FullName), 5, typeof(string)),
                new SchemaColumn(nameof(FileInfo.Exists), 6, typeof(bool)),
                new SchemaColumn(nameof(FileInfo.IsReadOnly), 7, typeof(bool)),
                new SchemaColumn(nameof(FileInfo.Length), 8, typeof(long)),
                new SchemaColumn("Content", 9, typeof(string)),
                new SchemaColumn(nameof(FileInfo), 10, typeof(FileInfo))
            };
        }

        private static string GetFileContent(FileInfo fileInfo)
        {
            if (!fileInfo.Exists)
                return null;

            using (var file = fileInfo.OpenRead())
            using (var fileReader = new StreamReader(file))
            {
                return fileReader.ReadToEnd();
            }
        }
    }
}