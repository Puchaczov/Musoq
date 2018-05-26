using System;
using System.Collections.Generic;
using System.IO;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Os.Process
{
    public static class ProcessHelper
    {
        public static readonly IDictionary<string, int> ProcessNameToIndexMap;
        public static readonly IDictionary<int, Func<System.Diagnostics.Process, object>> ProcessIndexToMethodAccessMap;
        public static readonly ISchemaColumn[] ProcessColumns;

        static ProcessHelper()
        {
            ProcessNameToIndexMap = new Dictionary<string, int>
            {
                {nameof(System.Diagnostics.Process.BasePriority), 0},
                {nameof(System.Diagnostics.Process.EnableRaisingEvents), 1},
                {nameof(System.Diagnostics.Process.ExitCode), 2},
                {nameof(System.Diagnostics.Process.ExitTime), 3},
                {nameof(System.Diagnostics.Process.Handle), 4},
                {nameof(System.Diagnostics.Process.HandleCount), 5},
                {nameof(System.Diagnostics.Process.HasExited), 6},
                {nameof(System.Diagnostics.Process.Id), 7},
                {nameof(System.Diagnostics.Process.MachineName), 8},
                {nameof(System.Diagnostics.Process.MainWindowTitle), 9},
                {nameof(System.Diagnostics.Process.PagedMemorySize64), 10},
                {nameof(System.Diagnostics.Process.ProcessName), 11},
                {nameof(System.Diagnostics.Process.ProcessorAffinity), 12},
                {nameof(System.Diagnostics.Process.Responding), 13},
                {nameof(System.Diagnostics.Process.StartTime), 14},
                {nameof(System.Diagnostics.Process.TotalProcessorTime), 15},
                {nameof(System.Diagnostics.Process.UserProcessorTime), 16},
                {"Dir", 17},
                {"FileName", 18}
            };

            ProcessIndexToMethodAccessMap = new Dictionary<int, Func<System.Diagnostics.Process, object>>
            {
                {0, info => info.BasePriority},
                {1, info => info.EnableRaisingEvents},
                {2, info => info.ExitCode},
                {3, info => info.ExitTime},
                {4, info => info.Handle},
                {5, info => info.HandleCount},
                {6, info => info.HasExited},
                {7, info => info.Id},
                {8, info => info.MachineName},
                {9, info => info.MainWindowTitle},
                {10, info => info.PagedMemorySize64},
                {11, info => info.ProcessName},
                {12, info => info.ProcessorAffinity},
                {13, info => info.Responding},
                {14, info => info.StartTime},
                {15, info => info.TotalProcessorTime},
                {16, info => info.UserProcessorTime},
                {
                    17, info =>
                    {
                        try
                        {
                            return new FileInfo(info.MainModule.FileName).Directory.FullName;
                        }
                        catch (Exception)
                        {
                            return "None";
                        }
                    }
                },
                {
                    18, info =>
                    {
                        try
                        {
                            return new FileInfo(info.MainModule.FileName).Name;
                        }
                        catch (Exception)
                        {
                            return "None";
                        }
                    }
                }
            };

            ProcessColumns = new ISchemaColumn[]
            {
                new SchemaColumn(nameof(System.Diagnostics.Process.BasePriority), 0, typeof(int)),
                new SchemaColumn(nameof(System.Diagnostics.Process.EnableRaisingEvents), 1, typeof(bool)),
                new SchemaColumn(nameof(System.Diagnostics.Process.ExitCode), 2, typeof(int)),
                new SchemaColumn(nameof(System.Diagnostics.Process.ExitTime), 3, typeof(DateTime)),
                new SchemaColumn(nameof(System.Diagnostics.Process.Handle), 4, typeof(IntPtr)),
                new SchemaColumn(nameof(System.Diagnostics.Process.HandleCount), 5, typeof(int)),
                new SchemaColumn(nameof(System.Diagnostics.Process.HasExited), 6, typeof(bool)),
                new SchemaColumn(nameof(System.Diagnostics.Process.Id), 7, typeof(int)),
                new SchemaColumn(nameof(System.Diagnostics.Process.MachineName), 8, typeof(string)),
                new SchemaColumn(nameof(System.Diagnostics.Process.MainWindowTitle), 9, typeof(string)),
                new SchemaColumn(nameof(System.Diagnostics.Process.PagedMemorySize64), 10, typeof(long)),
                new SchemaColumn(nameof(System.Diagnostics.Process.ProcessName), 11, typeof(string)),
                new SchemaColumn(nameof(System.Diagnostics.Process.ProcessorAffinity), 12, typeof(IntPtr)),
                new SchemaColumn(nameof(System.Diagnostics.Process.Responding), 13, typeof(bool)),
                new SchemaColumn(nameof(System.Diagnostics.Process.StartTime), 14, typeof(DateTime)),
                new SchemaColumn(nameof(System.Diagnostics.Process.TotalProcessorTime), 15, typeof(TimeSpan)),
                new SchemaColumn(nameof(System.Diagnostics.Process.UserProcessorTime), 16, typeof(TimeSpan)),
                new SchemaColumn("Dir", 17, typeof(string))
            };
        }
    }
}