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

    private static ISchemaProvider CreateTypedNumericSampleProvider()
    {
        return new TypedNumericSampleProvider();
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

        public override RowSource<T> GetRowSource<T, TArguments>(
            string name,
            SourceExecutionContext executionContext,
            TArguments nativeArguments)
        {
            var parameters = global::Musoq.Tests.Common.NativeSourceTestArguments.Read(nativeArguments);
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

    private sealed class TypedNumericSampleProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new TypedNumericSampleSchema();
        }
    }

    private sealed class TypedNumericSampleSchema : SchemaBase
    {
        public TypedNumericSampleSchema()
            : base("object", CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            return new TypedNumericSampleTable();
        }

        public override RowSource<T> GetRowSource<T, TArguments>(
            string name,
            SourceExecutionContext executionContext,
            TArguments nativeArguments)
        {
            return EnsureSourceType<T, TypedNumericSampleEntity>(
                name,
                new TypedNumericSampleRowSource());
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            methodsManager.RegisterLibraries(new Library());
            return new MethodsAggregator(methodsManager);
        }
    }

    private sealed class TypedNumericSampleTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(TypedNumericSampleEntity.Label), 0, typeof(string)),
            new SchemaColumn(nameof(TypedNumericSampleEntity.Value), 1, typeof(decimal?))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(TypedNumericSampleEntity));

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

    private sealed class TypedNumericSampleRowSource : RowSourceBase<TypedNumericSampleEntity>
    {
        protected override void CollectChunks(IChunkWriter<TypedNumericSampleEntity> writer)
        {
            writer.Write(
            [
                new TypedNumericSampleEntity { Label = "object", Value = 42 }
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

public sealed class TypedNumericSampleEntity
{
    public string Label { get; init; } = string.Empty;

    public decimal? Value { get; init; }
}
