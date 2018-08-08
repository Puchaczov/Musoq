using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Musoq.Schema.DataSources;
using Musoq.Schema.Os.Files;

namespace Musoq.Schema.Os.Tests.Core.Utils
{
    class TestFilesSource : FilesSource
    {
        public TestFilesSource(string path, bool useSubDirectories, InterCommunicator communicator) 
            : base(path, useSubDirectories, communicator)
        {
        }

        public IReadOnlyList<EntityResolver<FileInfo>> GetFiles()
        {
            var collection = new BlockingCollection<IReadOnlyList<EntityResolver<FileInfo>>>();
            CollectChunks(collection);

            var list = new List<EntityResolver<FileInfo>>();

            foreach(var item in collection)
                list.AddRange(item);

            return list;
        }
    }
}
