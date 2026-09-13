using System.Text.Json;
using Musoq.Benchmarks.Performance;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class LikeSpecializationQualificationGateTests
{
    private string _directory = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "musoq-like-gate", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [TestMethod]
    public void Evaluate_WhenAllThresholdsPass_ShouldSucceed()
    {
        var localBaseline = WriteCohort("local-baseline", 100d, 100d, 100d);
        var localCurrent = WriteCohort("local-current", 94d, 100d, 100d);
        var candidateBaseline = WriteCandidateCohort("candidate-baseline", 100d);
        var candidateCurrent = WriteCandidateCohort("candidate-current", 80d);

        var result = LikeSpecializationQualificationGate.Evaluate(
            localBaseline,
            localCurrent,
            candidateBaseline,
            candidateCurrent);

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public void Evaluate_WhenContainsOrCandidateMissesThreshold_ShouldFail()
    {
        var localBaseline = WriteCohort("local-baseline", 100d, 100d, 100d);
        var localCurrent = WriteCohort("local-current", 96d, 100d, 100d);
        var candidateBaseline = WriteCandidateCohort("candidate-baseline", 100d);
        var candidateCurrent = WriteCandidateCohort("candidate-current", 81d);

        var result = LikeSpecializationQualificationGate.Evaluate(
            localBaseline,
            localCurrent,
            candidateBaseline,
            candidateCurrent);

        Assert.IsFalse(result.IsSuccess);
    }

    private string[] WriteCohort(string prefix, double contains, double fallback, double allocations)
    {
        return Enumerable.Range(1, 3)
            .Select(index => WriteReport(
                $"{prefix}-{index}",
                ("Like_Contains(RowsCount: 100000)", contains, allocations),
                ("Like_Fallback_DynamicPattern(RowsCount: 100000)", fallback, allocations)))
            .ToArray();
    }

    private string[] WriteCandidateCohort(string prefix, double mean)
    {
        return Enumerable.Range(1, 3)
            .Select(index => WriteReport(
                $"{prefix}-{index}",
                ("CandidatePayload_SourcePlanningOff(RowsCount: 100000)", 100d, 100d),
                ("CandidatePayload_SourcePlanningOn(RowsCount: 100000)", mean, 100d)))
            .ToArray();
    }

    private string WriteReport(string name, params (string Method, double Mean, double Allocated)[] benchmarks)
    {
        var path = Path.Combine(_directory, $"{name}.json");
        var report = new
        {
            Benchmarks = benchmarks.Select(benchmark => new
            {
                FullName = benchmark.Method,
                Statistics = new { benchmark.Mean },
                Memory = new { BytesAllocatedPerOperation = benchmark.Allocated }
            })
        };
        File.WriteAllText(path, JsonSerializer.Serialize(report));
        return path;
    }
}
