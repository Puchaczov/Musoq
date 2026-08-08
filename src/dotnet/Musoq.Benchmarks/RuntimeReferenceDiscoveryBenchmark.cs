using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Benchmarks;

/// <summary>
/// Characterization benchmark for demand-driven CLR reference discovery.
/// Keep the sources empty so process visibility and host data do not affect the measurements.
/// </summary>
[MemoryDiagnoser]
public class RuntimeReferenceDiscoveryBenchmark : BenchmarkBase
{
    private const string PrimitiveQuery = "select 1 from #reference.values()";
    private const string ProcessQuery =
        "select ProcessName, ProcessorAffinity from #reference.processes()";
    private const string MultiAssemblyQuery =
        "select r.Name, r.Number, r.Payload.Uri.AbsoluteUri, r.Payload.Map.Count from #reference.multi() r";

    private readonly BenchmarkLoggerResolver _loggerResolver = new();
    private readonly CompilationOptions _options =
        BenchmarkCompilationOptions.Materialized(new CompilationOptions(usePrimitiveTypeValidation: false));
    private readonly string[] _coldProcessQueries =
    [
        ProcessQuery + " where 'cold-0' = 'cold-0'",
        ProcessQuery + " where 'cold-1' = 'cold-1'",
        ProcessQuery + " where 'cold-2' = 'cold-2'",
        ProcessQuery + " where 'cold-3' = 'cold-3'"
    ];

    private ReferenceSchemaProvider _provider = null!;

    [GlobalSetup]
    public void Setup()
    {
        _provider = new ReferenceSchemaProvider();
        InstanceCreator.CompileForExecution(
            ProcessQuery,
            "RuntimeReferenceDiscoveryBenchmark.Warmup",
            _provider,
            _loggerResolver,
            _options);
    }

    [Benchmark(Description = "Primitive control compilation")]
    public CompiledQuery CompilePrimitiveControl() =>
        InstanceCreator.CompileForExecution(
            PrimitiveQuery,
            "RuntimeReferenceDiscoveryBenchmark.Primitive",
            _provider,
            _loggerResolver,
            _options);

    [Benchmark(Description = "Process-backed compilation")]
    public CompiledQuery CompileProcessBacked() =>
        InstanceCreator.CompileForExecution(
            ProcessQuery,
            "RuntimeReferenceDiscoveryBenchmark.Process",
            _provider,
            _loggerResolver,
            _options);

    [Benchmark(Description = "Multi-assembly generic/inheritance compilation")]
    public CompiledQuery CompileMultiAssemblyGenericInheritance() =>
        InstanceCreator.CompileForExecution(
            MultiAssemblyQuery,
            "RuntimeReferenceDiscoveryBenchmark.MultiAssembly",
            _provider,
            _loggerResolver,
            _options);

    [Benchmark(Description = "Repeated process reference cache hit")]
    public CompiledQuery CompileRepeatedCacheHit() =>
        InstanceCreator.CompileForExecution(
            ProcessQuery,
            "RuntimeReferenceDiscoveryBenchmark.Warmup",
            _provider,
            _loggerResolver,
            _options);

    [Benchmark(Description = "Concurrent cold process compilation")]
    public int CompileConcurrentCold()
    {
        var generatedLengths = new int[_coldProcessQueries.Length];
        Parallel.For(0, _coldProcessQueries.Length, index =>
        {
            using var compiled = InstanceCreator.CompileForExecution(
                _coldProcessQueries[index],
                $"RuntimeReferenceDiscoveryBenchmark.Concurrent{index}",
                _provider,
                _loggerResolver,
                _options);
            generatedLengths[index] = compiled.GetType().FullName?.Length ?? 0;
        });

        return generatedLengths.Sum();
    }

    private sealed class ReferenceSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema) => new ReferenceSchema();
    }

    private sealed class ReferenceSchema : SchemaBase
    {
        public ReferenceSchema()
            : base("reference", CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            return name switch
            {
                "values" => new PrimitiveTable(),
                "processes" => new ProcessTable(),
                "multi" => new MultiAssemblyTable(),
                _ => throw new NotSupportedException($"Table {name} is not supported.")
            };
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            return name switch
            {
                "values" => EnsureSourceType<T, int>(name, new EmptyRowSource<int>()),
                "processes" => EnsureSourceType<T, Process>(name, new EmptyRowSource<Process>()),
                "multi" => EnsureSourceType<T, MultiAssemblyRow>(name, new EmptyRowSource<MultiAssemblyRow>()),
                _ => throw new NotSupportedException($"Table {name} is not supported.")
            };
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodManager = new MethodsManager();
            methodManager.RegisterLibraries(new LibraryBase());
            return new MethodsAggregator(methodManager);
        }
    }

    private sealed class PrimitiveTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [new SchemaColumn("Value", 0, typeof(int))];

        public SchemaTableMetadata Metadata { get; } = new(typeof(int));

        public ISchemaColumn? GetColumnByName(string name) =>
            string.Equals(name, "Value", StringComparison.OrdinalIgnoreCase) ? Columns[0] : null;

        public ISchemaColumn[] GetColumnsByName(string name) =>
            GetColumnByName(name) is { } column ? [column] : [];
    }

    private sealed class ProcessTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(Process.ProcessName), 0, typeof(string)),
            new SchemaColumn("ProcessorAffinity", 1, typeof(nint))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(Process));

        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.FirstOrDefault(column =>
                string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));

        public ISchemaColumn[] GetColumnsByName(string name) =>
            GetColumnByName(name) is { } column ? [column] : [];
    }

    private sealed class MultiAssemblyTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(MultiAssemblyRow.Name), 0, typeof(string)),
            new SchemaColumn(nameof(MultiAssemblyRow.Number), 1, typeof(int)),
            new SchemaColumn(nameof(MultiAssemblyRow.Payload), 2, typeof(MultiAssemblyPayload))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(MultiAssemblyRow));

        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.FirstOrDefault(column =>
                string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));

        public ISchemaColumn[] GetColumnsByName(string name) =>
            GetColumnByName(name) is { } column ? [column] : [];
    }

    private sealed class EmptyRowSource<T> : RowSourceBase<T>
    {
        protected override void CollectChunks(IChunkWriter<T> writer)
        {
        }
    }

    public abstract class MultiAssemblyBase
    {
        public string Name { get; init; } = string.Empty;
    }

    public sealed class MultiAssemblyRow : MultiAssemblyBase
    {
        public int Number { get; init; }

        public MultiAssemblyPayload Payload { get; init; } = new();
    }

    public sealed class MultiAssemblyPayload
    {
        public Uri Uri { get; init; } = new("https://example.invalid");

        public Dictionary<string, Uri> Map { get; init; } = [];
    }
}
