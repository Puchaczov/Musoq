using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Os.Zip
{
    public class ZipSource : RowSource
    {
        private readonly string _zipPath;
        private readonly RuntimeContext _communicator;

        public ZipSource(string zipPath, RuntimeContext communicator)
        {
            _zipPath = zipPath;
            _communicator = communicator;
        }

        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                var endWorkToken = _communicator.EndWorkToken;
                using (var file = File.OpenRead(_zipPath))
                {
                    using (var zip = new ZipArchive(file))
                    {
                        foreach (var entry in zip.Entries)
                        {
                            endWorkToken.ThrowIfCancellationRequested();
                            if (entry.Name != string.Empty)
                            {
                                yield return new EntityResolver<ZipArchiveEntry>(
                                    entry,
                                    SchemaZipHelper.NameToIndexMap,
                                    SchemaZipHelper.IndexToMethodAccessMap);
                            }
                        }
                    }
                }
            }
        }
    }
}