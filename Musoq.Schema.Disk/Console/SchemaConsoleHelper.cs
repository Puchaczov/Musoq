using System;
using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Disk.Console
{
    public static class SchemaConsoleHelper
    {
        static SchemaConsoleHelper()
        {
            Columns = new ISchemaColumn[]
            {
                new SchemaColumn(nameof(ConsoleEntity.Content), 0, typeof(string))
            };

            NameToIndexMap = new Dictionary<string, int>()
            {
                {nameof(ConsoleEntity.Content), 0}
            };

            IndexToMethodAccessMap = new Dictionary<int, Func<ConsoleEntity, object>>
            {
                {0, (entity) => entity.Content}
            };
        }

        public static ISchemaColumn[] Columns { get; }
        public static IDictionary<string, int> NameToIndexMap { get; }
        public static IDictionary<int, Func<ConsoleEntity, object>> IndexToMethodAccessMap { get; }
    }
}