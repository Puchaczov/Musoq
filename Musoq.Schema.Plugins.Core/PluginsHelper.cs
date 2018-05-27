using System;
using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Plugins.Core
{
    public static class PluginsHelper
    {
        public static readonly IDictionary<string, int> PluginNameToIndexMap;
        public static readonly IDictionary<int, Func<PluginEntity, object>> PluginIndexToMethodAccessMap;
        public static readonly ISchemaColumn[] PluginColumns;

        static PluginsHelper()
        {
            PluginNameToIndexMap = new Dictionary<string, int>
            {
                {nameof(PluginEntity.FullName), 0},
                {nameof(PluginEntity.Location), 1},
                {nameof(PluginEntity.SchemaName), 2}
            };

            PluginIndexToMethodAccessMap = new Dictionary<int, Func<PluginEntity, object>>
            {
                {0, info => info.FullName},
                {1, info => info.Location},
                {2, info => info.SchemaName}
            };

            PluginColumns = new ISchemaColumn[]
            {
                new SchemaColumn(nameof(PluginEntity.FullName), 0, typeof(string)),
                new SchemaColumn(nameof(PluginEntity.Location), 1, typeof(string)),
                new SchemaColumn(nameof(PluginEntity.SchemaName), 2, typeof(string))
            };
        }
    }
}