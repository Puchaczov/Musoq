using System.Text.Json;
using Musoq.Benchmarks.Performance;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class LoopInvariantQualificationGateTests
{
    private string _directory = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "musoq-loop-invariant-gate", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [TestMethod]
    public void Evaluate_WhenThreeCompleteCohortsMeetThresholds_ShouldPass()
    {
        var reports = WriteCohort(
            onTime: 95d,
            offTime: 100d,
            onAllocation: 10d,
            offAllocation: 10d);

        var result = LoopInvariantQualificationGate.Evaluate(reports);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(18, result.Comparisons);
        Assert.IsEmpty(result.Failures);
        Assert.AreEqual(0.95d, result.Comparisons.Single(comparison =>
            comparison.Scenario == "StableExpensiveGetter" && comparison.Fanout == 64).TimeRatio, 0.0001d);
    }

    [TestMethod]
    public void Evaluate_WhenExpensiveHighFanoutMissesRatio_ShouldFail()
    {
        var reports = WriteCohort(onTime: 98d, offTime: 100d, onAllocation: 10d, offAllocation: 10d);

        var result = LoopInvariantQualificationGate.Evaluate(reports);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(string.Join(Environment.NewLine, result.Failures), "StableExpensiveGetter/fanout=64");
    }

    [TestMethod]
    public void Evaluate_WhenAllocationIncreases_ShouldFailEvenWithFasterTiming()
    {
        var reports = WriteCohort(onTime: 90d, offTime: 100d, onAllocation: 2049d, offAllocation: 1024d);

        var result = LoopInvariantQualificationGate.Evaluate(reports);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(string.Join(Environment.NewLine, result.Failures), "allocation");
    }

    [TestMethod]
    public void Evaluate_WhenAReportIsIncomplete_ShouldRejectIt()
    {
        var reports = WriteCohort(onTime: 95d, offTime: 100d, onAllocation: 10d, offAllocation: 10d);
        var incomplete = Path.Combine(_directory, "incomplete.json");
        File.WriteAllText(incomplete, JsonSerializer.Serialize(new
        {
            Benchmarks = new[]
            {
                new
                {
                    FullName = "ExecuteOff(Fanout: 1, Scenario: StableCheapGetter)",
                    Statistics = new { Mean = 100d },
                    Memory = new { BytesAllocatedPerOperation = 10d }
                }
            }
        }));
        reports[2] = incomplete;

        var result = LoopInvariantQualificationGate.Evaluate(reports);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(string.Join(Environment.NewLine, result.Failures), "Missing complete benchmark row");
    }

    [TestMethod]
    public void Command_WhenThreeReportsAreProvided_ShouldEmitMachineReadableResult()
    {
        var reports = WriteCohort(onTime: 95d, offTime: 100d, onAllocation: 10d, offAllocation: 10d);
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = LoopInvariantQualificationGateCommand.Run(
            reports.SelectMany(path => new[] { "--report", path }).ToArray(),
            output,
            error);

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output.ToString(), "\"IsSuccess\":true");
        Assert.IsEmpty(error.ToString());
    }

    private string[] WriteCohort(double onTime, double offTime, double onAllocation, double offAllocation)
    {
        return Enumerable.Range(1, LoopInvariantQualificationGate.RequiredReports)
            .Select(index => WriteReport($"cohort-{index}", onTime, offTime, onAllocation, offAllocation))
            .ToArray();
    }

    private string WriteReport(string name, double onTime, double offTime, double onAllocation, double offAllocation)
    {
        var benchmarks = new List<object>();
        foreach (var scenario in new[]
                 {
                     "StableCheapGetter", "StableExpensiveGetter", "VolatileGetter",
                     "StableCheapCallable", "StableExpensiveCallable", "VolatileCallable"
                 })
        foreach (var fanout in new[] { 1, 8, 64 })
        foreach (var enabled in new[] { false, true })
        {
            benchmarks.Add(new
            {
                FullName = $"Execute{(enabled ? "On" : "Off")}(Fanout: {fanout}, Scenario: {scenario})",
                Statistics = new { Mean = enabled ? onTime : offTime },
                Memory = new { BytesAllocatedPerOperation = enabled ? onAllocation : offAllocation }
            });
        }

        var path = Path.Combine(_directory, $"{name}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new { Benchmarks = benchmarks }));
        return path;
    }
}
