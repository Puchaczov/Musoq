using System;
using System.Collections.Generic;
using System.IO;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Os.Directories
{
    public static class SchemaDirectoriesHelper
    {
        public static readonly IDictionary<string, int> DirectoriesNameToIndexMap;
        public static readonly IDictionary<int, Func<DirectoryInfo, object>> DirectoriesIndexToMethodAccessMap;
        public static readonly ISchemaColumn[] DirectoriesColumns;

        static SchemaDirectoriesHelper()
        {
            DirectoriesNameToIndexMap = new Dictionary<string, int>
            {
                {nameof(DirectoryInfo.FullName), 0},
                {nameof(DirectoryInfo.Attributes), 1},
                {nameof(DirectoryInfo.CreationTime), 2},
                {nameof(DirectoryInfo.CreationTimeUtc), 3},
                {nameof(DirectoryInfo.Exists), 4},
                {nameof(DirectoryInfo.Extension), 5},
                {nameof(DirectoryInfo.LastAccessTime), 6},
                {nameof(DirectoryInfo.LastAccessTimeUtc), 7},
                {nameof(DirectoryInfo.Name), 8},
                {nameof(DirectoryInfo.LastWriteTime), 9},
                {nameof(DirectoryInfo.Parent), 10},
                {nameof(DirectoryInfo.Root), 11},
                {nameof(DirectoryInfo), 12}
            };

            DirectoriesIndexToMethodAccessMap = new Dictionary<int, Func<DirectoryInfo, object>>
            {
                {0, info => info.FullName},
                {1, info => info.Attributes},
                {2, info => info.CreationTime},
                {3, info => info.CreationTimeUtc},
                {4, info => info.Exists},
                {5, info => info.Extension},
                {6, info => info.LastAccessTime},
                {7, info => info.LastAccessTimeUtc},
                {8, info => info.Name},
                {9, info => info.LastWriteTime},
                {10, info => info.Parent},
                {11, info => info.Root},
                {12, info => info}
            };

            DirectoriesColumns = new ISchemaColumn[]
            {
                new SchemaColumn(nameof(DirectoryInfo.FullName), 0, typeof(string)),
                new SchemaColumn(nameof(DirectoryInfo.Attributes), 1, typeof(FileAttributes)),
                new SchemaColumn(nameof(DirectoryInfo.CreationTime), 2, typeof(DateTime)),
                new SchemaColumn(nameof(DirectoryInfo.CreationTimeUtc), 3, typeof(DateTime)),
                new SchemaColumn(nameof(DirectoryInfo.Exists), 4, typeof(bool)),
                new SchemaColumn(nameof(DirectoryInfo.Extension), 5, typeof(string)),
                new SchemaColumn(nameof(DirectoryInfo.LastAccessTime), 6, typeof(DateTime)),
                new SchemaColumn(nameof(DirectoryInfo.LastAccessTimeUtc), 7, typeof(DateTime)),
                new SchemaColumn(nameof(DirectoryInfo.Name), 8, typeof(string)),
                new SchemaColumn(nameof(DirectoryInfo.LastWriteTime), 9, typeof(DateTime)),
                new SchemaColumn(nameof(DirectoryInfo.Parent), 10, typeof(DirectoryInfo)),
                new SchemaColumn(nameof(DirectoryInfo.Root), 11, typeof(DirectoryInfo)),
                new SchemaColumn(nameof(DirectoryInfo), 12, typeof(DirectoryInfo))
            };
        }
    }
}