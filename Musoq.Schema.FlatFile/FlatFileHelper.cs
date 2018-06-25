using System;
using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.FlatFile
{
    public static class FlatFileHelper
    {
        public static readonly IDictionary<string, int> FlatNameToIndexMap;
        public static readonly IDictionary<int, Func<FlatFileEntity, object>> FlatIndexToMethodAccessMap;
        public static readonly ISchemaColumn[] FlatColumns;

        static FlatFileHelper()
        {
            FlatNameToIndexMap = new Dictionary<string, int>
            {
                {nameof(FlatFileEntity.LineNumber), 0},
                {nameof(FlatFileEntity.Line), 1}
            };

            FlatIndexToMethodAccessMap = new Dictionary<int, Func<FlatFileEntity, object>>
            {
                {0, info => info.LineNumber},
                {1, info => info.Line}
            };

            FlatColumns = new ISchemaColumn[]
            {
                new SchemaColumn(nameof(FlatFileEntity.LineNumber), 0, typeof(int)),
                new SchemaColumn(nameof(FlatFileEntity.Line), 1, typeof(string))
            };
        }
    }
}