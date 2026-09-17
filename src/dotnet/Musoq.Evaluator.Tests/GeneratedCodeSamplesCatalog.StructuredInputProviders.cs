using Musoq.Examples.DataSources.StructuredInputs;
using Musoq.Evaluator.Tests.StructuredSamples;
using System;
using System.Linq;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static ISchemaProvider CreateStructuredInputsSchemaProvider()
    {
        return new StructuredInputsSchemaProvider();
    }

    private static ISchemaProvider CreateStructuredWideSchemaProvider()
    {
        return new StructuredWideSchemaProvider();
    }

    private sealed class StructuredWideSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (schema.Equals("structured", StringComparison.OrdinalIgnoreCase) ||
                schema.Equals("#structured", StringComparison.OrdinalIgnoreCase))
                return new StructuredWideSchema();

            throw new NotSupportedException(schema);
        }
    }

    private sealed class StructuredWideSchema : SchemaBase
    {
        public StructuredWideSchema()
            : base("structured", new MethodsAggregator(new MethodsManager()))
        {
            AddTable<StructuredWideTable>("wide");
            AddTypedSource<StructuredWideSource>("wide");
        }
    }

    private sealed class StructuredWideTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(StructuredWideRow.Value), 0, typeof(int))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(StructuredWideRow));

        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.SingleOrDefault(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));

        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

}