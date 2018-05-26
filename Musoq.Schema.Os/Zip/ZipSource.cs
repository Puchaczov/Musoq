using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Os.Zip
{
    public class ZipSource : RowSource
    {
        private readonly string _zipPath;

        public ZipSource(string zipPath)
        {
            _zipPath = zipPath;
        }

        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                using (var file = File.OpenRead(_zipPath))
                {
                    using (var zip = new ZipArchive(file))
                    {
                        foreach (var entry in zip.Entries)
                            if (entry.Name != string.Empty)
                                yield return new EntityResolver<ZipArchiveEntry>(entry, SchemaZipHelper.NameToIndexMap,
                                    SchemaZipHelper.IndexToMethodAccessMap);
                    }
                }
            }
        }
    }
}