using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Musoq.Schema.DataSources;
using Musoq.Schema.Os.Directories;

namespace Musoq.Schema.Os.Tests.Core.Utils
{
    class TestDirectoriesSource : DirectoriesSource
    {
        public TestDirectoriesSource(string path, bool recursive, RuntimeContext communicator) 
            : base(path, recursive, communicator)
        {
        }

        public IReadOnlyList<EntityResolver<DirectoryInfo>> GetDirectories()
        {
            var collection = new BlockingCollection<IReadOnlyList<EntityResolver<DirectoryInfo>>>();
            CollectChunks(collection);

            var list = new List<EntityResolver<DirectoryInfo>>();

            foreach (var item in collection)
                list.AddRange(item);

            return list;
        }
    }
}
