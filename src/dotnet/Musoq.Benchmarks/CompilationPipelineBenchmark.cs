using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Helpers;
using Musoq.Benchmarks.Schema;
using Musoq.Benchmarks.Schema.Country;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Benchmarks;

/// <summary>
///     Benchmarks the full query compilation pipeline (parse → transform → code gen → Roslyn emit).
/// </summary>
[Config(typeof(CompilationPipelineBenchmarkConfig))]
[MemoryDiagnoser]
public class CompilationPipelineBenchmark : BenchmarkBase
{
    private const string SimpleQuery =
        "select City, Country, Population from #A.Entities() where Population > 500000";

    private const string ComplexQuery =
        "select City, Country, Population, City + ' (' + Country + ')' as CityCountry from #A.Entities() where Population > 500000 group by City, Country, Population having Count(City) > 0 order by Population desc";

    private IDictionary<string, IEnumerable<CountryEntity>> _sources = null!;
    private readonly BenchmarkLoggerResolver _loggerResolver = new();
    private static readonly object MetricsLock = new();
    private static readonly Dictionary<string, CompilationPipelineMetricSnapshot> MetricSnapshots = [];

    [GlobalSetup]
    public void Setup()
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "countries.json");
        var data = DataHelpers.ParseCountryData(contentPath);
        _sources = new Dictionary<string, IEnumerable<CountryEntity>>
        {
            { "#A", data }
        };


        CreateForCountryWithOptions(SimpleQuery, _sources, new CompilationOptions());
    }

    [Benchmark(Description = "Simple query compile")]
    public CompiledQuery CompileSimpleQuery_Cold()
    {
        return CreateForCountryWithOptions(SimpleQuery, _sources, new CompilationOptions());
    }

    [Benchmark(Description = "Simple query compile (repeat)")]
    public CompiledQuery CompileSimpleQuery_Warm()
    {
        CreateForCountryWithOptions(SimpleQuery, _sources, new CompilationOptions());
        return CreateForCountryWithOptions(SimpleQuery, _sources, new CompilationOptions());
    }

    [Benchmark(Description = "Complex query compile")]
    public CompiledQuery CompileComplexQuery_Cold()
    {
        return CreateForCountryWithOptions(ComplexQuery, _sources, new CompilationOptions());
    }

    [Benchmark(Description = "Complex query compile (repeat)")]
    public CompiledQuery CompileComplexQuery_Warm()
    {
        CreateForCountryWithOptions(ComplexQuery, _sources, new CompilationOptions());
        return CreateForCountryWithOptions(ComplexQuery, _sources, new CompilationOptions());
    }

    [Benchmark(Description = "Simple generated C# chars")]
    public int MeasureSimpleGeneratedCodeLength()
    {
        return CompileForInspection(SimpleQuery).GeneratedCSharpCode.Length;
    }

    [Benchmark(Description = "Complex generated C# chars")]
    public int MeasureComplexGeneratedCodeLength()
    {
        return CompileForInspection(ComplexQuery).GeneratedCSharpCode.Length;
    }

    [Benchmark(Description = "Simple emitted DLL bytes")]
    public int MeasureSimpleAssemblyLength()
    {
        return CompileForStore(SimpleQuery).DllFile.Length;
    }

    [Benchmark(Description = "Complex emitted DLL bytes")]
    public int MeasureComplexAssemblyLength()
    {
        return CompileForStore(ComplexQuery).DllFile.Length;
    }

    private QueryInspectionResult CompileForInspection(string query)
    {
        return InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            CreateCountrySchemaProvider(),
            _loggerResolver,
            BenchmarkCompilationOptions.Materialized());
    }

    private (byte[] DllFile, byte[] PdbFile) CompileForStore(string query)
    {
        var items = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            CreateCountrySchemaProvider(),
            _loggerResolver,
            BenchmarkCompilationOptions.Materialized());

        return (
            items.DllFile ?? throw new InvalidOperationException("Compilation did not produce a DLL file."),
            items.PdbFile ?? throw new InvalidOperationException("Compilation did not produce a PDB file."));
    }

    private GenericSchemaProvider<CountryEntity, CountryEntityTable> CreateCountrySchemaProvider()
    {
        return new GenericSchemaProvider<CountryEntity, CountryEntityTable>(
            BenchmarkSourceChunks.FromRows(_sources),
            CountryEntity.KNameToIndexMap,
            CountryEntity.KIndexToObjectAccessMap);
    }

    internal static CompilationPipelineMetricSnapshot GetMetricSnapshot(string benchmarkMethodName)
    {
        var query = IsComplexBenchmark(benchmarkMethodName)
            ? ComplexQuery
            : SimpleQuery;

        lock (MetricsLock)
        {
            if (!MetricSnapshots.TryGetValue(query, out var snapshot))
            {
                snapshot = CreateMetricSnapshot(query);
                MetricSnapshots.Add(query, snapshot);
            }

            return snapshot;
        }
    }

    private static bool IsComplexBenchmark(string benchmarkMethodName)
    {
        return benchmarkMethodName.Contains("Complex", StringComparison.Ordinal);
    }

    private static CompilationPipelineMetricSnapshot CreateMetricSnapshot(string query)
    {
        var sources = CreateSources();
        var loggerResolver = new BenchmarkLoggerResolver();
        var generatedCodeLength = InstanceCreator.CompileForInspection(
                query,
                Guid.NewGuid().ToString(),
                CreateCountrySchemaProvider(sources),
                loggerResolver,
                BenchmarkCompilationOptions.Materialized())
            .GeneratedCSharpCode
            .Length;
        var items = InstanceCreator.CreateForAnalyze(
                query,
                Guid.NewGuid().ToString(),
                CreateCountrySchemaProvider(sources),
                loggerResolver,
                BenchmarkCompilationOptions.Materialized());
        var assemblyLength = (items.DllFile ?? throw new InvalidOperationException("Compilation did not produce a DLL file.")).Length;

        return new CompilationPipelineMetricSnapshot(generatedCodeLength, assemblyLength);
    }

    private static Dictionary<string, IEnumerable<CountryEntity>> CreateSources()
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "countries.json");
        var data = DataHelpers.ParseCountryData(contentPath);
        return new Dictionary<string, IEnumerable<CountryEntity>>
        {
            { "#A", data }
        };
    }

    private static GenericSchemaProvider<CountryEntity, CountryEntityTable> CreateCountrySchemaProvider(
        IDictionary<string, IEnumerable<CountryEntity>> sources)
    {
        return new GenericSchemaProvider<CountryEntity, CountryEntityTable>(
            BenchmarkSourceChunks.FromRows(sources),
            CountryEntity.KNameToIndexMap,
            CountryEntity.KIndexToObjectAccessMap);
    }
}

internal sealed record CompilationPipelineMetricSnapshot(int GeneratedCodeChars, int AssemblyBytes);

public sealed class CompilationPipelineBenchmarkConfig : ManualConfig
{
    public CompilationPipelineBenchmarkConfig()
    {
        AddColumn(new GeneratedCodeCharsColumn());
        AddColumn(new AssemblyBytesColumn());
    }
}

public sealed class GeneratedCodeCharsColumn : IColumn
{
    public string Id => nameof(GeneratedCodeCharsColumn);

    public string ColumnName => "Generated C#";

    public bool AlwaysShow => true;

    public ColumnCategory Category => ColumnCategory.Custom;

    public int PriorityInCategory => 0;

    public bool IsNumeric => true;

    public UnitType UnitType => UnitType.Size;

    public string Legend => "Generated C# source length in characters.";

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        return GetValue(summary, benchmarkCase, SummaryStyle.Default);
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        return CompilationPipelineBenchmark
            .GetMetricSnapshot(benchmarkCase.Descriptor.WorkloadMethod.Name)
            .GeneratedCodeChars
            .ToString("N0", CultureInfo.InvariantCulture);
    }

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase)
    {
        return false;
    }

    public bool IsAvailable(Summary summary)
    {
        return true;
    }
}

public sealed class AssemblyBytesColumn : IColumn
{
    public string Id => nameof(AssemblyBytesColumn);

    public string ColumnName => "DLL bytes";

    public bool AlwaysShow => true;

    public ColumnCategory Category => ColumnCategory.Custom;

    public int PriorityInCategory => 1;

    public bool IsNumeric => true;

    public UnitType UnitType => UnitType.Size;

    public string Legend => "Emitted DLL size in bytes.";

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        return GetValue(summary, benchmarkCase, SummaryStyle.Default);
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        return CompilationPipelineBenchmark
            .GetMetricSnapshot(benchmarkCase.Descriptor.WorkloadMethod.Name)
            .AssemblyBytes
            .ToString("N0", CultureInfo.InvariantCulture);
    }

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase)
    {
        return false;
    }

    public bool IsAvailable(Summary summary)
    {
        return true;
    }
}
