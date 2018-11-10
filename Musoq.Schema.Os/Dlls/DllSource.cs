using Musoq.Schema.DataSources;
using Musoq.Schema.Os.Files;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Musoq.Schema.Os.Dlls
{
    public class DllSource : FilesSourceBase<DllInfo>
    {
        public DllSource(string path, bool useSubDirectories, InterCommunicator communicator) 
            : base(path, useSubDirectories, communicator)
        {
        }

        protected override EntityResolver<DllInfo> CreateBasedOnFile(FileInfo file)
        {
            Assembly asm = null;
            try
            {
                asm = Assembly.LoadFrom(file.FullName);
            }
            catch
            {
                asm = null;
            }

            var version = FileVersionInfo.GetVersionInfo(asm.Location);
            return new EntityResolver<DllInfo>(new DllInfo
            {
                FileInfo = file,
                Assembly = asm,
                Version = version
            }, DllInfosHelper.DllInfosNameToIndexMap, DllInfosHelper.DllInfosIndexToMethodAccessMap);
        }

        protected override FileInfo[] GetFiles(DirectoryInfo directoryInfo)
        {
            return directoryInfo.GetFiles("*.dll");
        }
    }
}
