using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.ReadModifiers;
using Musoq.Evaluator.Tests.Schema.Unknown;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static ISchemaProvider CreateUnknownTableCoupleSampleProvider()
    {
        return new UnknownSchemaProvider(Array.Empty<dynamic>());
    }

    private static ISchemaProvider CreateTypedTypeMatrixSampleProvider()
    {
        return new TypedTypeMatrixSampleProvider();
    }

    private static ISchemaProvider CreateReadModifiersSampleProvider()
    {
        return new ReadModifiersSchemaProvider(
            Array.Empty<IReadOnlyDictionary<string, object?>>());
    }

    private static ISchemaProvider CreateTypedReadModifiersSampleProvider()
    {
        return new TypedReadModifiersSampleProvider();
    }

    private static ISchemaProvider CreateTableCoupleArgumentsSampleProvider()
    {
        return new TableCoupleArgumentsSampleProvider();
    }

    private static ISchemaProvider CreateSettingsProfileSampleProvider()
    {
        return new SourceRuntimeSettingsLifecycleTests.SettingsSchemaProvider(
            SourceRuntimeSettingsLifecycleTests.SettingsDeclarationMode.RequiredAndOptional);
    }

    private static CompilationOptions CreateSettingsProfileCompilationOptions()
    {
        return new CompilationOptions(
            sourceRuntimeSettingsResolver: new DeterministicSettingsProfileResolver())
            .WithStabilityAwareScalarReuse();
    }

    private static ISchemaProvider CreateTableCoupleSampleProvider()
    {
        return new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] =
                [
                    new BasicEntity { Id = 1, Name = "alpha", Population = 10m },
                    new BasicEntity { Id = 2, Name = "beta", Population = 20m }
                ],
                ["#B"] =
                [
                    new BasicEntity { Id = 1, Name = "alpha", Population = 11m },
                    new BasicEntity { Id = 3, Name = "gamma", Population = 30m }
                ]
            });
    }

    private sealed class TableCoupleArgumentsSampleProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (schema.Equals("named", StringComparison.OrdinalIgnoreCase) ||
                schema.Equals("#named", StringComparison.OrdinalIgnoreCase))
                return new NamedDatasourceSampleSchema();

            if (schema.Equals("unknown", StringComparison.OrdinalIgnoreCase) ||
                schema.Equals("#unknown", StringComparison.OrdinalIgnoreCase))
            {
                return new UnknownSchema(
                    [new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        ["Text"] = "cte"
                    }]);
            }

            throw new NotSupportedException(schema);
        }
    }

    private sealed class DeterministicSettingsProfileResolver : ISourceRuntimeSettingsResolver
    {
        public IReadOnlyDictionary<string, string> Resolve(
            SourceRuntimeSettingsResolutionRequest request)
        {
            var profile = string.IsNullOrWhiteSpace(request.ProfileName)
                ? "default"
                : request.ProfileName;
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["TOKEN"] = $"{profile}-token"
            };
        }
    }

    private sealed class TypedTypeMatrixSampleProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!schema.Equals("unknown", StringComparison.OrdinalIgnoreCase) &&
                !schema.Equals("#unknown", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException(schema);

            return new TypedTypeMatrixSampleSchema();
        }
    }

    private sealed class TypedTypeMatrixSampleSchema : SchemaBase
    {
        public TypedTypeMatrixSampleSchema()
            : base("unknown", new Musoq.Schema.Managers.MethodsAggregator(new Musoq.Schema.Managers.MethodsManager()))
        {
        }

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (!name.Equals("rows", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException(name);

            return new TypedTypeMatrixSampleTable();
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            if (!name.Equals("rows", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException(name);

            return EnsureSourceType<T, SpecificationTypeMatrixEntity>(
                name,
                new EmptyTypedRowSource<SpecificationTypeMatrixEntity>());
        }
    }

    private sealed class TypedTypeMatrixSampleTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.ByteCol), 0, typeof(byte)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.SByteCol), 1, typeof(sbyte)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.ShortCol), 2, typeof(short)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.IntCol), 3, typeof(int)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.LongCol), 4, typeof(long)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.UShortCol), 5, typeof(ushort)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.UIntCol), 6, typeof(uint)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.ULongCol), 7, typeof(ulong)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.FloatCol), 8, typeof(float)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.DoubleCol), 9, typeof(double)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.DecimalCol), 10, typeof(decimal)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.MoneyCol), 11, typeof(decimal)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.BoolCol), 12, typeof(bool)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.BitCol), 13, typeof(bool)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.CharCol), 14, typeof(char)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.StringCol), 15, typeof(string)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.DateTimeCol), 16, typeof(DateTime)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.DateTimeOffsetCol), 17, typeof(DateTimeOffset?)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.TimeSpanCol), 18, typeof(TimeSpan)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.GuidCol), 19, typeof(Guid)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.ObjectCol), 20, typeof(object)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.FullyQualified), 21, typeof(int)),
            new SchemaColumn(nameof(SpecificationTypeMatrixEntity.NullableInt), 22, typeof(int?))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(SpecificationTypeMatrixEntity));

        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.SingleOrDefault(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));

        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    private sealed class TypedReadModifiersSampleProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!schema.Equals("readmods", StringComparison.OrdinalIgnoreCase) &&
                !schema.Equals("#readmods", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException(schema);

            return new TypedReadModifiersSampleSchema();
        }
    }

    private sealed class TypedReadModifiersSampleSchema : SchemaBase
    {
        public TypedReadModifiersSampleSchema()
            : base("readmods", new Musoq.Schema.Managers.MethodsAggregator(new Musoq.Schema.Managers.MethodsManager()))
        {
        }

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (!name.Equals("records", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException(name);

            return new TypedReadModifiersSampleTable();
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            if (!name.Equals("records", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException(name);

            return EnsureSourceType<T, SpecificationReadModifiersEntity>(
                name,
                new EmptyTypedRowSource<SpecificationReadModifiersEntity>());
        }
    }

    private sealed class TypedReadModifiersSampleTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(
                nameof(SpecificationReadModifiersEntity.InvoiceNo),
                0,
                typeof(string),
                new Dictionary<string, string>
                {
                    [ColumnReadModifiers.Encoding] = "windows-1250",
                    [ColumnReadModifiers.Trim] = string.Empty
                }),
            new SchemaColumn(
                nameof(SpecificationReadModifiersEntity.CustomerName),
                1,
                typeof(string),
                new Dictionary<string, string>
                {
                    [ColumnReadModifiers.Encoding] = "windows-1250",
                    [ColumnReadModifiers.Trim] = string.Empty
                }),
            new SchemaColumn(
                nameof(SpecificationReadModifiersEntity.Total),
                2,
                typeof(decimal),
                new Dictionary<string, string>
                {
                    [ColumnReadModifiers.Culture] = "pl-PL",
                    [ColumnReadModifiers.Format] = "#,##0.00"
                }),
            new SchemaColumn(
                nameof(SpecificationReadModifiersEntity.Attachment),
                3,
                typeof(string),
                new Dictionary<string, string>
                {
                    [$"{ColumnReadModifiers.SourcePrefix}codec"] = "base64"
                })
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(SpecificationReadModifiersEntity));

        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.SingleOrDefault(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));

        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    private sealed class EmptyTypedRowSource<T> : RowSourceBase<T>
    {
        protected override void CollectChunks(IChunkWriter<T> writer)
        {
        }
    }
}

public sealed class SpecificationTypeMatrixEntity
{
    public byte ByteCol { get; init; }
    public sbyte SByteCol { get; init; }
    public short ShortCol { get; init; }
    public int IntCol { get; init; }
    public long LongCol { get; init; }
    public ushort UShortCol { get; init; }
    public uint UIntCol { get; init; }
    public ulong ULongCol { get; init; }
    public float FloatCol { get; init; }
    public double DoubleCol { get; init; }
    public decimal DecimalCol { get; init; }
    public decimal MoneyCol { get; init; }
    public bool BoolCol { get; init; }
    public bool BitCol { get; init; }
    public char CharCol { get; init; }
    public string StringCol { get; init; } = string.Empty;
    public DateTime DateTimeCol { get; init; }
    public DateTimeOffset? DateTimeOffsetCol { get; init; }
    public TimeSpan TimeSpanCol { get; init; }
    public Guid GuidCol { get; init; }
    public object? ObjectCol { get; init; }
    public int FullyQualified { get; init; }
    public int? NullableInt { get; init; }
}

public sealed class SpecificationReadModifiersEntity
{
    public string InvoiceNo { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public string Attachment { get; init; } = string.Empty;
}
