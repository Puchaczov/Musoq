using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private sealed class ScriptParameterSampleSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new ScriptParameterSampleSchema();
        }
    }

    private sealed class ScriptParameterSampleSchema()
        : SchemaBase("parameterized", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            return new ScriptParameterSampleTable();
        }

        public override RowSource<T> GetRowSource<T, TArguments>(
            string name,
            SourceExecutionContext executionContext,
            TArguments nativeArguments)
        {
            return EnsureSourceType<T, ScriptParameterSampleEntity>(
                name,
                new ScriptParameterSampleRowSource([]));
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            methodsManager.RegisterLibraries(new Library());
            return new MethodsAggregator(methodsManager);
        }
    }

    private sealed class ScriptParameterSampleTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(ScriptParameterSampleEntity.Key), 0, typeof(string)),
            new SchemaColumn(nameof(ScriptParameterSampleEntity.Value), 1, typeof(string))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(ScriptParameterSampleEntity));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }
    }

    private sealed class ScriptParameterSampleRowSource(IReadOnlyList<ScriptParameterSampleEntity> rows)
        : RowSourceBase<ScriptParameterSampleEntity>
    {
        protected override void CollectChunks(IChunkWriter<ScriptParameterSampleEntity> writer)
        {
            writer.Write(rows.ToArray());
        }
    }

}
