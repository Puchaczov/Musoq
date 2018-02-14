using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Schema.Zip
{
    public class ZipLibrary : LibraryBase
    {
        [AggregationSetMethod]
        public void SetAggregateFiles([InjectGroup] Group group, [InjectSource] FileInfo file, string name)
        {
            var list = group.GetOrCreateValue(name, new List<FileInfo>());

            list.Add(file);
        }

        [AggregationGetMethod]
        public IReadOnlyList<FileInfo> AggregateFiles([InjectGroup] Group group, string name)
        {
            return group.GetValue<IReadOnlyList<FileInfo>>(name);
        }

        [BindableMethod]
        public string Decompress(IReadOnlyList<FileInfo> files, string path)
        {
            if (files.Count == 0)
                return string.Empty;

            var operationSucessfull = true;

            try
            {
                foreach (var zipFile in files)
                {
                    using (var zipStream = zipFile.OpenRead())
                    {
                        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                        {
                            foreach (var entry in archive.Entries)
                            {
                                entry.ExtractToFile(Path.Combine(path, entry.FullName), true);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                operationSucessfull = false;
            }

            return operationSucessfull ? path : string.Empty;
        }
    }
}