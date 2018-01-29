using System;
using System.Collections.Generic;
using System.IO;
using FQL.Schema.DataSources;

namespace FQL.Schema.Disk.Disk
{
    public static class SchemaDiskHelper
    {
        public static readonly IDictionary<string, int> NameToIndexMap;
        public static readonly IDictionary<int, Func<FileInfo, object>> IndexToMethodAccessMap;
        public static readonly ISchemaColumn[] SchemaColumns;

        static SchemaDiskHelper()
        {
            NameToIndexMap = new Dictionary<string, int>
            {
                {nameof(FileInfo.Name), 0},
                {nameof(FileInfo.CreationTime), 1},
                {nameof(FileInfo.CreationTimeUtc), 2},
                {nameof(FileInfo.DirectoryName), 3},
                {nameof(FileInfo.Extension), 4},
                {nameof(FileInfo.FullName), 5},
                {nameof(FileInfo.Exists), 6},
                {nameof(FileInfo.IsReadOnly), 7},
                {nameof(FileInfo.Length), 8}
            };

            IndexToMethodAccessMap = new Dictionary<int, Func<FileInfo, object>>
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

            SchemaColumns = new ISchemaColumn[]
            {
                new SchemaColumn(nameof(FileInfo.Name), 0, typeof(string)),
                new SchemaColumn(nameof(FileInfo.CreationTime), 1, typeof(DateTime)),
                new SchemaColumn(nameof(FileInfo.CreationTimeUtc), 2, typeof(DateTime)),
                new SchemaColumn(nameof(FileInfo.DirectoryName), 3, typeof(string)),
                new SchemaColumn(nameof(FileInfo.Extension), 4, typeof(string)),
                new SchemaColumn(nameof(FileInfo.FullName), 5, typeof(string)),
                new SchemaColumn(nameof(FileInfo.Exists), 6, typeof(bool)),
                new SchemaColumn(nameof(FileInfo.IsReadOnly), 7, typeof(bool)),
                new SchemaColumn(nameof(FileInfo.Length), 8, typeof(long))
            };
        }
    }
}