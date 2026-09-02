using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static ISchemaProvider CreateNamedDatasourceSampleProvider()
    {
        return new NamedDatasourceSampleProvider();
    }

    private static ISchemaProvider CreateObjectCoercionSampleProvider()
    {
        return new ObjectCoercionSampleProvider();
    }

    private sealed class NamedDatasourceSampleProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new NamedDatasourceSampleSchema();
        }
    }

    private sealed class NamedDatasourceSampleSchema : SchemaBase
    {
        private static readonly SchemaMethodInfo[] Constructors =
        [
            new(
                "any",
                new SchemaConstructorInfo(
                    typeof(NamedDatasourceSampleTable)
                        .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(constructor => constructor.GetParameters().Length == 2),
                    false,
                    ("first", typeof(string)),
                    ("second", typeof(int))))
        ];

        public NamedDatasourceSampleSchema()
            : base("named", new MethodsAggregator(new MethodsManager()))
        {
        }

        public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext)
        {
            return Constructors;
        }

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            return new NamedDatasourceSampleTable(
                parameters.ElementAtOrDefault(0) as string ?? string.Empty,
                parameters.Length > 1 && parameters[1] is int second ? second : 7);
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            return EnsureSourceType<T, NamedDatasourceSampleEntity>(
                name,
                new NamedDatasourceSampleRowSource(parameters));
        }
    }

    private sealed class NamedDatasourceSampleTable(string first, int second = 7) : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(NamedDatasourceSampleEntity.Value), 0, typeof(int)),
            new SchemaColumn(nameof(NamedDatasourceSampleEntity.First), 1, typeof(string)),
            new SchemaColumn(nameof(NamedDatasourceSampleEntity.Second), 2, typeof(int))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(NamedDatasourceSampleEntity));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column =>
                column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column =>
                column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        public NamedDatasourceSampleEntity CreateEntity()
        {
            return new NamedDatasourceSampleEntity
            {
                Value = 1,
                First = first,
                Second = second
            };
        }
    }

    private sealed class NamedDatasourceSampleRowSource(object?[] parameters)
        : RowSourceBase<NamedDatasourceSampleEntity>
    {
        protected override void CollectChunks(IChunkWriter<NamedDatasourceSampleEntity> writer)
        {
            var first = parameters.ElementAtOrDefault(0) as string ?? string.Empty;
            var second = parameters.Length > 1 && parameters[1] is int value ? value : 7;
            writer.Write(
            [
                new NamedDatasourceSampleEntity
                {
                    Value = 1,
                    First = first,
                    Second = second
                }
            ]);
        }
    }

    private sealed class ObjectCoercionSampleProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new ObjectCoercionSampleSchema();
        }
    }

    private sealed class ObjectCoercionSampleSchema : SchemaBase
    {
        public ObjectCoercionSampleSchema()
            : base("object", CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            return new ObjectCoercionSampleTable();
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            return EnsureSourceType<T, ObjectCoercionSampleEntity>(
                name,
                new ObjectCoercionSampleRowSource());
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            methodsManager.RegisterLibraries(new Library());
            return new MethodsAggregator(methodsManager);
        }
    }

    private sealed class ObjectCoercionSampleTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(ObjectCoercionSampleEntity.Label), 0, typeof(string)),
            new SchemaColumn(nameof(ObjectCoercionSampleEntity.Value), 1, typeof(object))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(ObjectCoercionSampleEntity));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column =>
                column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column =>
                column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
    }

    private sealed class ObjectCoercionSampleRowSource : RowSourceBase<ObjectCoercionSampleEntity>
    {
        protected override void CollectChunks(IChunkWriter<ObjectCoercionSampleEntity> writer)
        {
            writer.Write(
            [
                new ObjectCoercionSampleEntity { Label = "object", Value = 42 }
            ]);
        }
    }
}

public sealed class NamedDatasourceSampleEntity
{
    public int Value { get; init; }

    public string First { get; init; } = string.Empty;

    public int Second { get; init; }
}

public sealed class ObjectCoercionSampleEntity
{
    public string Label { get; init; } = string.Empty;

    public object? Value { get; init; }
}
