using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Scenarios;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Benchmarks;

public abstract class CompilationCase
{
    protected Musoq.Schema.ISchemaProvider Provider = null!;
    protected Scenario Scenario = null!;
    protected CompilationOptions Options = null!;
    public IEnumerable<string> Scenarios => ScenarioCatalog.CompilationIds;
    [ParamsSource(nameof(Scenarios))]
    public string ScenarioId { get; set; } = "B01_Scan";

    protected void Prepare()
    {
        Scenario = ScenarioCatalog.Get(ScenarioId);
        MeasuredProcess.Record(Scenario, FixtureData.TestSize);
        Provider = FixtureCatalog.Prepare(Scenario, FixtureData.TestSize).CreateSchemaProvider();
        Options = ExecutionSettings.Create(Scenario, FixtureData.TestSize);
    }

    protected void Compile()
    {
        using var query = InstanceCreator.CompileForExecution(Scenario.Sql, "compilation", Provider, BenchmarkLogger.Instance, Options);
    }
}

// Each launch measures exactly one first compilation; no setup compilation and no memory rerun.
public class ColdCompilationBenchmark : CompilationCase
{
    [GlobalSetup]
    public void Setup() => Prepare();
    [Benchmark]
    public void CompileAndDispose() => Compile();
}

[MemoryDiagnoser]
public class WarmCompilationBenchmark : CompilationCase
{
    [GlobalSetup]
    public void Setup()
    {
        Prepare();
        Compile();
    }
    [Benchmark]
    public void CompileAndDispose() => Compile();
}

[MemoryDiagnoser]
public class ArtifactLoadBenchmark
{
    private Musoq.Schema.ISchemaProvider _provider = null!;
    private Scenario _scenario = null!;
    private ICompiledQueryArtifact _artifact = null!;
    public IEnumerable<string> Scenarios => ScenarioCatalog.ArtifactIds;
    [ParamsSource(nameof(Scenarios))]
    public string ScenarioId { get; set; } = "B14_Group";

    [GlobalSetup]
    public void Setup()
    {
        _scenario = ScenarioCatalog.Get(ScenarioId);
        MeasuredProcess.Record(_scenario, FixtureData.TestSize);
        _provider = FixtureCatalog.Prepare(_scenario, FixtureData.TestSize).CreateSchemaProvider();
        _artifact = QueryArtifacts.Create(_scenario, _provider);
    }

    [Benchmark]
    public void LoadAndDispose()
    {
        using var query = QueryArtifacts.Load(_scenario, _artifact, _provider);
    }
}

public static class QueryArtifacts
{
    public static ICompiledQueryArtifact Create(Scenario scenario, Musoq.Schema.ISchemaProvider provider)
    {
        var result = InstanceCreator.CompileArtifactWithDiagnostics(scenario.Sql, "artifact", provider,
            BenchmarkLogger.Instance, ExecutionSettings.Create(scenario, FixtureData.TestSize));
        return result.Artifact ?? throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
    }

    public static CompiledQuery Load(Scenario scenario, ICompiledQueryArtifact artifact, Musoq.Schema.ISchemaProvider provider)
    {
        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(scenario.Sql, artifact, provider,
            BenchmarkLogger.Instance, ExecutionSettings.Create(scenario, FixtureData.TestSize));
        return result.CompiledQuery ?? throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
    }
}
