using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Musoq.Schema.Solution
{
    public static class SchemaSolutionHelper
    {
        public static readonly IDictionary<string, int> NameToIndexMap;
        public static readonly IDictionary<int, Func<Document, object>> IndexToMethodAccessMap;
        public static readonly ISchemaColumn[] SchemaColumns;

        static SchemaSolutionHelper()
        {
            NameToIndexMap = new Dictionary<string, int>
            {
                {nameof(Document.Name), 0},
                {nameof(Document.Folders), 1},
                {nameof(Document.FilePath), 2},
                {"ProjectId", 3}
            };

            IndexToMethodAccessMap = new Dictionary<int, Func<Document, object>>
            {
                {0, document => document.Name },
                {1, document => document.Folders },
                {2, document => document.FilePath }
            };
        }
    }
}