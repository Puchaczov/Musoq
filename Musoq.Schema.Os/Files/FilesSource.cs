using System.IO;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Os.Files
{
    public class FilesSource : FilesSourceBase<FileInfo>
    {
        public FilesSource(string path, bool useSubDirectories, RuntimeContext communicator) 
            : base(path, useSubDirectories, communicator)
        {
        }

        protected override EntityResolver<FileInfo> CreateBasedOnFile(FileInfo file)
        {
            return new EntityResolver<FileInfo>(file, SchemaFilesHelper.FilesNameToIndexMap,
                            SchemaFilesHelper.FilesIndexToMethodAccessMap);
        }
    }
}