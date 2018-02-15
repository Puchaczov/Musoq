using System;
using System.Collections.Generic;
using System.IO;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Disk
{
    public static class SchemaDiskHelper
    {
        public static readonly IDictionary<string, int> FilesNameToIndexMap;
        public static readonly IDictionary<int, Func<FileInfo, object>> FilesIndexToMethodAccessMap;
        public static readonly IDictionary<string, int> DirectoriesNameToIndexMap;
        public static readonly IDictionary<int, Func<DirectoryInfo, object>> DirectoriesIndexToMethodAccessMap;
        public static readonly ISchemaColumn[] FilesColumns;
        public static readonly ISchemaColumn[] DirectoriesColumns;

        static SchemaDiskHelper()
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
                {nameof(FileInfo.Length), 8}
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
                {8, info => info.Length}
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
                new SchemaColumn(nameof(FileInfo.Length), 8, typeof(long))
            };

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
                new SchemaColumn(nameof(DirectoryInfo.Attributes), 0, typeof(FileAttributes)),
                new SchemaColumn(nameof(DirectoryInfo.CreationTime), 0, typeof(DateTime)),
                new SchemaColumn(nameof(DirectoryInfo.CreationTimeUtc), 0, typeof(DateTime)),
                new SchemaColumn(nameof(DirectoryInfo.Exists), 0, typeof(bool)),
                new SchemaColumn(nameof(DirectoryInfo.Extension), 0, typeof(string)),
                new SchemaColumn(nameof(DirectoryInfo.LastAccessTime), 0, typeof(DateTime)),
                new SchemaColumn(nameof(DirectoryInfo.LastAccessTimeUtc), 0, typeof(DateTime)),
                new SchemaColumn(nameof(DirectoryInfo.Name), 0, typeof(string)),
                new SchemaColumn(nameof(DirectoryInfo.LastWriteTime), 0, typeof(DateTime)),
                new SchemaColumn(nameof(DirectoryInfo.Parent), 0, typeof(DirectoryInfo)),
                new SchemaColumn(nameof(DirectoryInfo.Root), 0, typeof(DirectoryInfo)),
                new SchemaColumn(nameof(DirectoryInfo), 0, typeof(DirectoryInfo))
            };
        }
    }
}