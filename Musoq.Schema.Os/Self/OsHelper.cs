using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Os.Self
{
    public static class OsHelper
    {
        public static readonly IDictionary<string, int> ProcessNameToIndexMap;
        public static readonly IDictionary<int, Func<object[], object>> ProcessIndexToMethodAccessMap;
        public static readonly ISchemaColumn[] ProcessColumns;

        public class Environment
        {
            public string OperatingSystem => System.Environment.OSVersion.VersionString;

            public int ProcessorCount => System.Environment.ProcessorCount;

            public string Processor => "Some Processor";

            public float Memory => 1024;

            public string EntryDirectory
            {
                get
                {
                    var dir = new DirectoryInfo(Assembly.GetEntryAssembly().Location);
                    return dir.FullName;
                }
            }

            public string RoslynVersion => "1.2.3.4";
        }

        static OsHelper()
        {
            ProcessNameToIndexMap = new Dictionary<string, int>
            {
                {"Key", 0},
                {"Value", 1}
            };

            ProcessIndexToMethodAccessMap = new Dictionary<int, Func<object[], object>>
            {
                {0, info => info[0]},
                {1, info => info[1]}
            };

            ProcessColumns = new ISchemaColumn[]
            {
                new SchemaColumn("Key", 0, typeof(string)),
                new SchemaColumn("Value", 1, typeof(object)),
            };
        }
    }
}