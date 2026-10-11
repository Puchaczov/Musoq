using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Benchmarks;

public enum FinalOutputScenario
{
    Projection,
    Join,
    Aggregate,
    Window
}

/// <summary>Output-heavy execution and compilation measured independently with identical inputs.</summary>
[MemoryDiagnoser]
public class FinalOutputAllocationBenchmark
{
    private readonly ILoggerResolver _logger = new BenchmarkLoggerResolver();
    private ISchemaProvider _provider = null!;
    private CompiledQuery _query = null!;
    private string _sql = null!;

    [Params(1_000, 100_000)]
    public int RowsCount { get; set; }

    [ParamsAllValues]
    public FinalOutputScenario Scenario { get; set; }

    [Params(false, true)]
    public bool Wide { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rows = Enumerable.Range(0, RowsCount).Select(index => new EvaluatorPerformanceEntity
        {
            Id = index,
            Name = $"Name_{index}",
            City = $"City_{index % 64}",
            Country = $"Country_{index % 4}",
            Population = index,
            Month = $"Month_{index % 12}",
            Money = index * 1.25m,
            NullableValue = index % 5 == 0 ? null : index
        }).ToArray();
        _provider = new EvaluatorPerformanceSchemaProvider(
            new Dictionary<string, IReadOnlyList<EvaluatorPerformanceEntity>>
            {
                ["#A"] = rows,
                ["#B"] = rows.SelectMany(static row => Enumerable.Repeat(row, 4)).ToArray()
            });
        _sql = QueryText(Scenario, Wide);
        _query = CompileQuery();
        var expected = Scenario == FinalOutputScenario.Join ? RowsCount * 4 : RowsCount;
        if (_query.Run().Count != expected)
            throw new InvalidOperationException("Unexpected benchmark cardinality.");
    }

    [GlobalCleanup]
    public void Cleanup() => _query.Dispose();

    [Benchmark]
    public int Execute() => _query.Run().Count;

    [Benchmark]
    public void Compile()
    {
        using var query = CompileQuery();
    }

    private CompiledQuery CompileQuery() => InstanceCreator.CompileForExecution(
        _sql, "FinalOutputAllocation", _provider, _logger,
        new CompilationOptions(ParallelizationMode.None,
            sourceRuntimeSettingsResolver: NonDefaultSourceRuntimeSettingsResolver.Instance));

    public static string QueryText(FinalOutputScenario scenario, bool wide)
    {
        const string fields = "a.Id, a.Name, a.City, a.Country, a.Population, a.Month, a.Money, a.NullableValue";
        return scenario switch
        {
            FinalOutputScenario.Projection =>
                $"select {(wide ? fields : "a.Id, a.Population")} from #A.entities() a where a.Population >= 0",
            FinalOutputScenario.Join =>
                $"select {(wide ? fields : "a.Id, b.Population")} from #A.entities() a inner join #B.entities() b on a.Id = b.Id",
            FinalOutputScenario.Aggregate => wide
                ? "select a.Id, a.Name, a.City, a.Country, a.Sum(a.Population), " +
                  "a.Month, a.Sum(a.Money), a.Max(a.NullableValue) from #A.entities() a " +
                  "group by a.Id, a.Name, a.City, a.Country, a.Month"
                : "select a.Id, a.Sum(a.Population) from #A.entities() a group by a.Id",
            FinalOutputScenario.Window =>
                $"select {(wide ? fields : "a.Id")}, row_number() over (order by a.Id) as Position from #A.entities() a",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
    }
}
