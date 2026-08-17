using System.Text.Json;
using Musoq.Benchmarks.Performance;
using Musoq.Evaluator;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class RecursiveCteBenchmarkGateTests
{
    private static readonly RecursiveCteBenchmarkScenario[] RuntimeScenarios =
        Enum.GetValues<RecursiveCteBenchmarkScenario>();

    private string _directory = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "musoq-recursive-gate", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [TestMethod]
    public void Compare_WhenEightHandwrittenEquivalentScenariosStayWithinLimits_ShouldPass()
    {
        var reports = WriteRuntimeCohort("equivalence", musoqMean: 124, musoqAllocation: 119);

        var result = RecursiveCteBenchmarkGate.Compare(reports);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(8, result.Comparisons);
        Assert.IsTrue(result.Comparisons.All(static comparison => comparison.Method.StartsWith("Sequential/")));
    }

    [TestMethod]
    public void Compare_WhenGeneratedExecutionExceedsTimeLimit_ShouldFail()
    {
        var reports = WriteRuntimeCohort("regression", musoqMean: 126, musoqAllocation: 119);

        var result = RecursiveCteBenchmarkGate.Compare(reports);

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.Comparisons.All(static comparison => comparison.IsRegression));
    }

    [TestMethod]
    public void CompareTiered_WhenAllCohortsStayWithinLimits_ShouldPassEveryTier()
    {
        var inputs = WriteTieredInputs(currentMean: 102, currentAllocation: 102);

        var result = RecursiveCteBenchmarkGate.CompareTiered(inputs);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(6, result.Tiers);
        CollectionAssert.AreEqual(
            new[]
            {
                "sequential-equivalence",
                "full-mode-regression",
                "correlated-apply-overhead",
                "empty-anchor-overhead",
                "recursive-compilation-regression",
                "ordinary-cte-regression"
            },
            result.Tiers.Select(static tier => tier.Name).ToArray());
    }

    [TestMethod]
    public void CompareTiered_WhenBeforeAfterCohortExceedsThreePercent_ShouldFail()
    {
        var inputs = WriteTieredInputs(currentMean: 104, currentAllocation: 100);

        var result = RecursiveCteBenchmarkGate.CompareTiered(inputs);

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.Tiers.Skip(1).All(static tier => !tier.Result.IsSuccess));
    }

    [TestMethod]
    public void Command_WhenEquivalenceReportsPass_ShouldPrintRatios()
    {
        var reports = WriteRuntimeCohort("command", musoqMean: 110, musoqAllocation: 110);
        var output = new StringWriter();
        var error = new StringWriter();
        var args = reports.SelectMany(static report => new[] { "--report", report }).ToArray();

        var exitCode = RecursiveCteBenchmarkGateCommand.Run(args, output, error);

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output.ToString(), "[sequential-equivalence]");
        StringAssert.Contains(output.ToString(), "Recursive CTE performance gate passed.");
        Assert.AreEqual(string.Empty, error.ToString());
    }

    [TestMethod]
    public void Command_WhenTieredReportsPass_ShouldPrintEveryTier()
    {
        var inputs = WriteTieredInputs(currentMean: 102, currentAllocation: 102);
        var output = new StringWriter();
        var error = new StringWriter();
        var args = CreateTieredArguments(inputs);

        var exitCode = RecursiveCteBenchmarkGateCommand.Run(args, output, error);

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output.ToString(), "[full-mode-regression]");
        StringAssert.Contains(output.ToString(), "[recursive-compilation-regression]");
        StringAssert.Contains(output.ToString(), "[ordinary-cte-regression]");
        Assert.AreEqual(string.Empty, error.ToString());
    }

    private RecursiveCtePerformanceGateInputs WriteTieredInputs(double currentMean, double currentAllocation) =>
        new(
            WriteRuntimeCohort("runtime-baseline", 100, 100),
            WriteRuntimeCohort("runtime-current", currentMean, currentAllocation),
            WriteCompilationCohort("compilation-baseline", 100, 100),
            WriteCompilationCohort("compilation-current", currentMean, currentAllocation),
            WriteOrdinaryCohort("ordinary-baseline", 100, 100),
            WriteOrdinaryCohort("ordinary-current", currentMean, currentAllocation));

    private string[] WriteRuntimeCohort(string name, double musoqMean, double musoqAllocation) =>
        Enumerable.Range(1, RecursiveCteBenchmarkGate.RequiredSamples)
            .Select(index => WriteRuntimeReport($"{name}-{index}", musoqMean, musoqAllocation))
            .ToArray();

    private string[] WriteCompilationCohort(string name, double mean, double allocation) =>
        Enumerable.Range(1, RecursiveCteBenchmarkGate.RequiredSamples)
            .Select(index => WriteReport(
                $"{name}-{index}",
                from scenario in new[]
                {
                    RecursiveCteBenchmarkScenario.Chain,
                    RecursiveCteBenchmarkScenario.WideRows,
                    RecursiveCteBenchmarkScenario.IndexedEdges
                }
                from mode in new[] { ParallelizationMode.None, ParallelizationMode.Full }
                select CreateRecord(
                    nameof(RecursiveCteCompilationBenchmark),
                    nameof(RecursiveCteCompilationBenchmark.Compile),
                    $"Scenario: {scenario}, Scale: 1024, ExecutionMode: {mode}",
                    mean,
                    allocation)))
            .ToArray();

    private string[] WriteOrdinaryCohort(string name, double mean, double allocation) =>
        Enumerable.Range(1, RecursiveCteBenchmarkGate.RequiredSamples)
            .Select(index => WriteReport(
                $"{name}-{index}",
                new[]
                {
                    CreateRecord(nameof(CteSidecarIndexBenchmark), "SingleHash_Baseline", "RowsCount: 32", mean, allocation),
                    CreateRecord(nameof(CteSidecarIndexBenchmark), "SingleHash_Sidecar", "RowsCount: 32", mean, allocation)
                }))
            .ToArray();

    private string WriteRuntimeReport(string name, double musoqMean, double musoqAllocation)
    {
        var benchmarks = new List<object>();
        foreach (var scenario in RuntimeScenarios)
        foreach (var mode in new[] { ParallelizationMode.None, ParallelizationMode.Full })
        {
            var parameters = $"Scenario: {scenario}, Scale: 1024, ExecutionMode: {mode}";
            benchmarks.Add(CreateRecord(
                nameof(RecursiveCteBenchmark),
                nameof(RecursiveCteBenchmark.HandwrittenSemiNaive),
                parameters,
                100,
                100));
            benchmarks.Add(CreateRecord(
                nameof(RecursiveCteBenchmark),
                nameof(RecursiveCteBenchmark.MusoqGenerated),
                parameters,
                musoqMean,
                musoqAllocation));
        }

        return WriteReport(name, benchmarks);
    }

    private string WriteReport(string name, IEnumerable<object> benchmarks)
    {
        var path = Path.Combine(_directory, $"{name}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new { Benchmarks = benchmarks }));
        return path;
    }

    private static object CreateRecord(
        string benchmark,
        string method,
        string parameters,
        double mean,
        double allocated) => new
    {
        Method = method,
        FullName = $"Musoq.Benchmarks.{benchmark}.{method}({parameters})",
        Statistics = new { Mean = mean },
        Memory = new { BytesAllocatedPerOperation = allocated }
    };

    private static string[] CreateTieredArguments(RecursiveCtePerformanceGateInputs inputs) =>
    [
        .. AddArguments("--baseline-runtime", inputs.BaselineRuntimeReports),
        .. AddArguments("--current-runtime", inputs.CurrentRuntimeReports),
        .. AddArguments("--baseline-compilation", inputs.BaselineCompilationReports),
        .. AddArguments("--current-compilation", inputs.CurrentCompilationReports),
        .. AddArguments("--baseline-ordinary", inputs.BaselineOrdinaryCteReports),
        .. AddArguments("--current-ordinary", inputs.CurrentOrdinaryCteReports)
    ];

    private static IEnumerable<string> AddArguments(string option, IEnumerable<string> reports) =>
        reports.SelectMany(report => new[] { option, report });
}
