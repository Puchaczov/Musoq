using BenchmarkDotNet.Attributes;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class LibraryWindowCleanupBenchmark
{
    private const int RowCount = 10_000;
    private WindowFunctionBenchmark _fixture = null!;

    [Params(WindowFunctionBenchmark.WindowScenario.TypedPluginRunningProduct,
        WindowFunctionBenchmark.WindowScenario.TypedPluginWithArguments)]
    public WindowFunctionBenchmark.WindowScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _fixture = new WindowFunctionBenchmark { RowsCount = RowCount, Scenario = Scenario };
        _fixture.Setup();
        var random = new Random(42);
        var inputs = Enumerable.Range(0, RowCount)
            .Select(index => (Name: $"Employee{index}", Partition: index % 10, Salary: random.Next(30000, 200000)))
            .OrderBy(row => row.Name, StringComparer.Ordinal);
        var products = Enumerable.Repeat(1m, 10).ToArray();
        decimal expected = 0;
        foreach (var row in inputs)
        {
            products[row.Partition] = products[row.Partition] * (row.Salary % 11 + 1) % 1_000_000_007m;
            expected += products[row.Partition];
        }
        if (Scenario == WindowFunctionBenchmark.WindowScenario.TypedPluginWithArguments)
            expected *= 2m;
        if (Consume() != expected)
            throw new InvalidOperationException("Window cleanup result differs from the independent ordered reference.");
    }

    [Benchmark]
    public decimal Consume()
    {
        using var table = _fixture.RunQuery();
        decimal checksum = 0;
        foreach (var row in table)
            checksum += row.GetValue<decimal>(2);
        return checksum;
    }
}
