using System.Text.Json;
using Musoq.Benchmarks.Performance;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class FirstClassEnumQualificationGateTests
{
    private string _directory = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(
            Path.GetTempPath(),
            "musoq-enum-gate",
            Guid.NewGuid().ToString("N"));
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
            enumTime: 101d,
            carrierTime: 100d,
            enumAllocation: 1024d,
            carrierAllocation: 1024d);

        var result = FirstClassEnumQualificationGate.Evaluate(reports);

        Assert.IsTrue(result.IsSuccess, string.Join(Environment.NewLine, result.Failures));
        Assert.HasCount(Enum.GetValues<FirstClassEnumScenario>().Length, result.Comparisons);
        Assert.IsEmpty(result.Failures);
    }

    [TestMethod]
    public void Evaluate_WhenEqualityExceedsTwoPercent_ShouldFail()
    {
        var reports = WriteCohort(
            enumTime: 103d,
            carrierTime: 100d,
            enumAllocation: 1024d,
            carrierAllocation: 1024d);

        var result = FirstClassEnumQualificationGate.Evaluate(reports);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(string.Join(Environment.NewLine, result.Failures), "Equality");
    }

    [TestMethod]
    public void Evaluate_WhenEnumAddsPerRowAllocation_ShouldFail()
    {
        var reports = WriteCohort(
            enumTime: 100d,
            carrierTime: 100d,
            enumAllocation: 10_000d,
            carrierAllocation: 1024d);

        var result = FirstClassEnumQualificationGate.Evaluate(reports);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(string.Join(Environment.NewLine, result.Failures), "allocation");
    }

    [TestMethod]
    public void Evaluate_WhenAReportIsIncomplete_ShouldRejectIt()
    {
        var reports = WriteCohort(100d, 100d, 1024d, 1024d);
        var incomplete = Path.Combine(_directory, "incomplete.json");
        File.WriteAllText(incomplete, JsonSerializer.Serialize(new
        {
            Benchmarks = new[]
            {
                BenchmarkRow("ExecuteCarrier", FirstClassEnumScenario.Equality, 100d, 1024d)
            }
        }));
        reports[2] = incomplete;

        var result = FirstClassEnumQualificationGate.Evaluate(reports);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(string.Join(Environment.NewLine, result.Failures), "Missing complete benchmark row");
    }

    [TestMethod]
    public void Evaluate_WhenAReportPathIsRepeated_ShouldRejectIt()
    {
        var reports = WriteCohort(100d, 100d, 1024d, 1024d);

        var exception = Assert.Throws<ArgumentException>(() =>
            FirstClassEnumQualificationGate.Evaluate(
                [reports[0], reports[0], reports[2]]));

        StringAssert.Contains(exception.Message, "distinct BenchmarkDotNet reports");
    }

    [TestMethod]
    public void Evaluate_ShouldTakeMedianOfPairedCohortRatios()
    {
        var reports = new[]
        {
            WriteReport("paired-1", new BenchmarkPairMetrics(101d, 100d, 110d, 100d)),
            WriteReport("paired-2", new BenchmarkPairMetrics(100d, 200d, 100d, 200d)),
            WriteReport("paired-3", new BenchmarkPairMetrics(306d, 300d, 306d, 300d))
        };

        var result = FirstClassEnumQualificationGate.Evaluate(reports);
        var equality = result.Comparisons.Single(static comparison =>
            comparison.Scenario == FirstClassEnumScenario.Equality);

        Assert.AreEqual(1.01d, equality.TimeRatio, 0.000001d);
        Assert.AreEqual(6d, equality.AllocationDeltaBytes);
    }

    [TestMethod]
    public void Command_WhenThreeReportsAreProvided_ShouldEmitMachineReadableResult()
    {
        var reports = WriteCohort(100d, 100d, 1024d, 1024d);
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = FirstClassEnumQualificationGateCommand.Run(
            reports.SelectMany(static path => new[] { "--report", path }).ToArray(),
            output,
            error);

        Assert.AreEqual(0, exitCode, error.ToString());
        StringAssert.Contains(output.ToString(), "\"IsSuccess\":true");
        Assert.IsEmpty(error.ToString());
    }

    private string[] WriteCohort(
        double enumTime,
        double carrierTime,
        double enumAllocation,
        double carrierAllocation)
    {
        var metrics = new BenchmarkPairMetrics(
            enumTime,
            carrierTime,
            enumAllocation,
            carrierAllocation);
        return Enumerable.Range(1, FirstClassEnumQualificationGate.RequiredReports)
            .Select(index => WriteReport(
                $"cohort-{index}",
                metrics))
            .ToArray();
    }

    private string WriteReport(
        string name,
        BenchmarkPairMetrics metrics)
    {
        var benchmarks = Enum.GetValues<FirstClassEnumScenario>()
            .SelectMany(scenario => new[]
            {
                BenchmarkRow("ExecuteCarrier", scenario, metrics.CarrierTime, metrics.CarrierAllocation),
                BenchmarkRow("ExecuteEnum", scenario, metrics.EnumTime, metrics.EnumAllocation)
            })
            .ToArray();
        var path = Path.Combine(_directory, $"{name}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new { Benchmarks = benchmarks }));
        return path;
    }

    private static object BenchmarkRow(
        string method,
        FirstClassEnumScenario scenario,
        double time,
        double allocation)
    {
        return new
        {
            FullName = $"{method}(RowsCount: {FirstClassEnumQualificationGate.RowsPerOperation}, Scenario: {scenario})",
            Statistics = new { Mean = time },
            Memory = new { BytesAllocatedPerOperation = allocation }
        };
    }

    private readonly record struct BenchmarkPairMetrics(
        double EnumTime,
        double CarrierTime,
        double EnumAllocation,
        double CarrierAllocation);
}
