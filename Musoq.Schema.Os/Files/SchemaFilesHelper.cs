using System;
using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Os.Files
{
    public static class SchemaFilesHelper
    {
        public static readonly IDictionary<string, int> FilesNameToIndexMap;
        public static readonly IDictionary<int, Func<ExtendedFileInfo, object>> FilesIndexToMethodAccessMap;
        public static readonly ISchemaColumn[] FilesColumns;

        static SchemaFilesHelper()
        {
            FilesNameToIndexMap = new Dictionary<string, int>
            {
                {nameof(ExtendedFileInfo.Name), 0},
                {nameof(ExtendedFileInfo.CreationTime), 1},
                {nameof(ExtendedFileInfo.CreationTimeUtc), 2},
                {nameof(ExtendedFileInfo.DirectoryName), 3},
                {nameof(ExtendedFileInfo.Extension), 4},
                {nameof(ExtendedFileInfo.FullName), 5},
                {nameof(ExtendedFileInfo.Exists), 6},
                {nameof(ExtendedFileInfo.IsReadOnly), 7},
                {nameof(ExtendedFileInfo.Length), 8}
            };

            FilesIndexToMethodAccessMap = new Dictionary<int, Func<ExtendedFileInfo, object>>
            {
                {0, info => info.Name},
                {1, info => info.CreationTime},
                {2, info => info.CreationTimeUtc},
                {3, info => info.DirectoryName},
                {4, info => info.Extension},
                {5, info => info.FullName},
                {6, info => info.Exists},
                {7, info => info.IsReadOnly},
                {8, info => info.Length}
            };

            FilesColumns = new ISchemaColumn[]
            {
                new SchemaColumn(nameof(ExtendedFileInfo.Name), 0, typeof(string)),
                new SchemaColumn(nameof(ExtendedFileInfo.CreationTime), 1, typeof(DateTime)),
                new SchemaColumn(nameof(ExtendedFileInfo.CreationTimeUtc), 2, typeof(DateTime)),
                new SchemaColumn(nameof(ExtendedFileInfo.DirectoryName), 3, typeof(string)),
                new SchemaColumn(nameof(ExtendedFileInfo.Extension), 4, typeof(string)),
                new SchemaColumn(nameof(ExtendedFileInfo.FullName), 5, typeof(string)),
                new SchemaColumn(nameof(ExtendedFileInfo.Exists), 6, typeof(bool)),
                new SchemaColumn(nameof(ExtendedFileInfo.IsReadOnly), 7, typeof(bool)),
                new SchemaColumn(nameof(ExtendedFileInfo.Length), 8, typeof(long))
            };
        }
    }
}