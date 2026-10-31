using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Generated;
using Musoq.Evaluator.Tests.Schema.RuntimeV2;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{

    internal static BasicSchemaProvider<BasicEntity> CreateBasicSchemaProvider()
    {
        return new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                { "#A", Array.Empty<BasicEntity>() },
                { "#B", Array.Empty<BasicEntity>() },
                { "#C", Array.Empty<BasicEntity>() }
            });
    }

    private static GeneratedApplySampleSchemaProvider CreateGeneratedApplySchemaProvider()
    {
        return new GeneratedApplySampleSchemaProvider(
        [
            new GeneratedApplySampleEntity
            {
                Name = "left",
                Numbers = [1, 2]
            },
            new GeneratedApplySampleEntity
            {
                Name = "right",
                Numbers = [3]
            }
        ]);
    }

    private static RuntimeV2RegressionSchemaProvider CreateRuntimeV2RegressionSchemaProvider()
    {
        return new RuntimeV2RegressionSchemaProvider([]);
    }

    private static BenchmarkParitySchemaProvider CreateBenchmarkParitySchemaProvider()
    {
        return new BenchmarkParitySchemaProvider([]);
    }

    private static RuntimeV2CastGroupingFeatureSchemaProvider CreateRuntimeV2CastGroupingFeatureSchemaProvider()
    {
        return new RuntimeV2CastGroupingFeatureSchemaProvider([]);
    }

    private static WeatherMeasurementSchemaProvider CreateWeatherMeasurementSchemaProvider()
    {
        return new WeatherMeasurementSchemaProvider(WeatherMeasurementEntity.EmptyRows);
    }

    private static ScriptParameterSampleSchemaProvider CreateScriptParameterSampleSchemaProvider()
    {
        return new ScriptParameterSampleSchemaProvider();
    }

    private static GeneratedApplySampleSchemaProvider CreateMixedDistinctGeneratedApplySchemaProvider()
    {
        return new GeneratedApplySampleSchemaProvider(
        [
            new GeneratedApplySampleEntity
            {
                Name = "left",
                Numbers = [1, 1, 4]
            },
            new GeneratedApplySampleEntity
            {
                Name = "right",
                Numbers = [3]
            }
        ]);
    }

    private static DynamicSampleSchemaProvider CreateDynamicAsOfSchemaProvider()
    {
        return new DynamicSampleSchemaProvider(
            new Dictionary<string, Type>
            {
                ["Team"] = typeof(string),
                ["Name"] = typeof(string),
                ["Score"] = typeof(int)
            },
            [
                new Dictionary<string, object>
                {
                    ["Team"] = "red",
                    ["Name"] = "ada",
                    ["Score"] = 2
                },
                new Dictionary<string, object>
                {
                    ["Team"] = "blue",
                    ["Name"] = "bea",
                    ["Score"] = 1
                },
                new Dictionary<string, object>
                {
                    ["Team"] = "red",
                    ["Name"] = "cid",
                    ["Score"] = 3
                }
            ]);
    }

    private sealed class GeneratedApplySampleSchemaProvider(IReadOnlyList<GeneratedApplySampleEntity> rows)
        : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!string.Equals(schema, "apply", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(schema, "#apply", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException(schema);

            return new GeneratedApplySampleSchema(rows);
        }
    }

    private sealed class GeneratedApplySampleSchema(IReadOnlyList<GeneratedApplySampleEntity> rows)
        : SchemaBase("apply", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (string.Equals(name, "items", StringComparison.OrdinalIgnoreCase))
                return new GeneratedApplySampleTable();

            throw new NotSupportedException(name);
        }

        public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext executionContext, TArguments nativeArguments)
        {
            if (string.Equals(name, "items", StringComparison.OrdinalIgnoreCase))
                return EnsureSourceType<T, GeneratedApplySampleEntity>(name, new GeneratedApplySampleRowSource(rows));

            throw new NotSupportedException(name);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            methodsManager.RegisterLibraries(new Library());
            return new MethodsAggregator(methodsManager);
        }
    }

    private sealed class GeneratedApplySampleTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(GeneratedApplySampleEntity.Name), 0, typeof(string)),
            new SchemaColumn(nameof(GeneratedApplySampleEntity.Numbers), 1, typeof(int[]))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(GeneratedApplySampleEntity));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }
    }

    private sealed class GeneratedApplySampleRowSource(IReadOnlyList<GeneratedApplySampleEntity> rows)
        : RowSourceBase<GeneratedApplySampleEntity>
    {
        protected override void CollectChunks(IChunkWriter<GeneratedApplySampleEntity> writer)
        {
            writer.Write(rows.ToArray());
        }
    }

    private sealed class DynamicSampleSchemaProvider(
        IReadOnlyDictionary<string, Type> columns,
        IReadOnlyList<IReadOnlyDictionary<string, object>> rows)
        : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new Musoq.Evaluator.Tests.Schema.Dynamic.DynamicSchema(columns, rows.Cast<dynamic>());
        }
    }

    private sealed class InterpretationSchemaProviderFactory : BinaryOrTextualEvaluatorTestBase
    {
        public static ISchemaProvider CreateBinary()
        {
            return new BinarySchemaProvider(
                new Dictionary<string, IEnumerable<BinaryEntity>>
                {
                    { "#test", Array.Empty<BinaryEntity>() }
                });
        }

        public static ISchemaProvider CreateText()
        {
            return new TextSchemaProvider(
                new Dictionary<string, IEnumerable<TextEntity>>
                {
                    { "#test", Array.Empty<TextEntity>() }
                });
        }
    }
    private sealed class BenchmarkSampleSchemaProvider(int inputCount) : ISchemaProvider
    {
        private readonly BenchmarkSampleSchema _schema = new(inputCount);

        public ISchema GetSchema(string schema)
        {
            if (!string.Equals(schema.TrimStart('#'), "bench", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Unknown sample schema '{schema}'.");
            return _schema;
        }
    }

    private sealed class BenchmarkSampleSchema : SchemaBase
    {
        private readonly Dictionary<string, BenchmarkSampleRows> _sources;

        public BenchmarkSampleSchema(int inputCount) : base("bench", CreateSampleLibrary())
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inputCount);
            var rows = Enumerable.Range(0, inputCount).Select(CreateBenchmarkRow).ToArray();
            var stream = rows.SelectMany(row => row.Payload).Concat(rows[0].Truncated).ToArray();
            _sources = new Dictionary<string, BenchmarkSampleRows>(StringComparer.OrdinalIgnoreCase)
            {
                ["rows"] = new(rows),
                ["lookup"] = new(rows.Take(3).ToArray()),
                ["stream"] = new([rows[0] with { Payload = stream }])
            };
        }

        public override ISchemaTable GetTableByName(string name, SourceMetadataContext context,
            params object?[] parameters)
        {
            if (!_sources.ContainsKey(name))
                throw new InvalidOperationException($"Unknown sample source '{name}'.");
            return new BenchmarkSampleTable();
        }

        public override RowSource<T> GetRowSource<T, TArguments>(string name,
            SourceExecutionContext context, TArguments arguments) =>
            EnsureSourceType<T, BenchmarkSampleRow>(name, _sources[name]);

        private static MethodsAggregator CreateSampleLibrary()
        {
            var methods = new MethodsManager();
            methods.RegisterLibraries(new Musoq.Plugins.LibraryBase());
            return new MethodsAggregator(methods);
        }
    }

    private static BenchmarkSampleRow CreateBenchmarkRow(int index)
    {
        var payload = new byte[8];
        payload[0] = (byte)(index % 4);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(1), index + 1);
        payload[5] = 2;
        payload[6] = 10;
        payload[7] = 11;
        return new BenchmarkSampleRow(index, index % 4, index + 1, $"item-{index % 4}",
            index % 3 == 0 ? null : index, [index, index + 1], payload,
            $"INFO|{index}|item-{index % 4}", payload[..3]);
    }

    private sealed class BenchmarkSampleTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = typeof(BenchmarkSampleRow).GetProperties()
            .Select((property, index) => (ISchemaColumn)new SchemaColumn(property.Name, index, property.PropertyType))
            .ToArray();
        public SchemaTableMetadata Metadata { get; } = new(typeof(BenchmarkSampleRow));
        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.FirstOrDefault(column => column.ColumnName == name);
        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column => column.ColumnName == name).ToArray();
    }

    private sealed class BenchmarkSampleRows(BenchmarkSampleRow[] rows) : RowSource<BenchmarkSampleRow>
    {
        private readonly BenchmarkSampleRow[][] _chunks = rows.Chunk(RowChunking.DefaultChunkSize).ToArray();
        public override IEnumerable<IReadOnlyList<BenchmarkSampleRow>> Chunks => _chunks;
    }
}

public sealed class ScriptParameterSampleEntity
{
    public string Key { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;
}

public sealed record BenchmarkSampleRow(int Id, int Key, decimal Amount, string Name, int? Optional,
    int[] Items, byte[] Payload, string Text, byte[] Truncated);

public readonly record struct BenchmarkProjectionOutput(int Id, string Name);
public readonly record struct BenchmarkWindowOutput(int Id, decimal? Total, decimal? First);
