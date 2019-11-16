using System.IO;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Os.Files
{
    public class FilesSource : FilesSourceBase<ExtendedFileInfo>
    {
        public FilesSource(string path, bool useSubDirectories, RuntimeContext communicator) 
            : base(path, useSubDirectories, communicator)
        {
        }

        public FilesSource(IReadOnlyTable table, RuntimeContext runtimeContext)
            : base(table, runtimeContext) 
        {
        }

        protected override EntityResolver<ExtendedFileInfo> CreateBasedOnFile(FileInfo file, string rootDirectory)
        {
            return new EntityResolver<ExtendedFileInfo>(new ExtendedFileInfo(file, rootDirectory), SchemaFilesHelper.FilesNameToIndexMap,
                            SchemaFilesHelper.FilesIndexToMethodAccessMap);
        }
    }
}