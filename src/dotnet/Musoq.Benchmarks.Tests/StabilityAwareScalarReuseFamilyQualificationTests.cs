using System.Text.Json;
using Musoq.Benchmarks.Performance;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class StabilityAwareScalarReuseFamilyQualificationTests
{
    [TestMethod]
    public void FamilyMatrix_ContainsAllTenOperatorSurfaces()
    {
        Assert.HasCount(10, Enum.GetValues<ScalarReuseFamily>());
        CollectionAssert.AreEquivalent(
            new[]
            {
                "CrossBoundaryProjection", "Windows", "AggregatesAndPivot", "GuardedApply",
                "SpecializedJoins", "CorrelatedProbes", "Unpivot", "BoundaryRowWidth",
                "ProviderProjection", "RecursiveCte"
            },
            Enum.GetNames<ScalarReuseFamily>());
    }

    [TestMethod]
    public void FamilyMatrix_CoversCandidateAndNoCandidateWorkloads()
    {
        CollectionAssert.AreEquivalent(
            new[] { "StableCheap", "StableExpensive", "Volatile", "NoCandidate" },
            Enum.GetNames<ScalarReuseWorkload>());
    }

    [TestMethod]
    public void FamilyBenchmark_UsesTheExistingCounterOracle()
    {
        var benchmark = new StabilityAwareScalarReuseFamilyQualificationBenchmark
        {
            Fanout = 8,
            Family = ScalarReuseFamily.Windows,
            Workload = ScalarReuseWorkload.StableExpensive
        };
        benchmark.Setup();
        try
        {
            var off = benchmark.ExecuteOff();
            var on = benchmark.ExecuteOn();
            Assert.AreEqual(off, on);
        }
        finally
        {
            benchmark.Cleanup();
        }
    }

    [TestMethod]
    public void FamilyGate_RequiresEveryFamilyWorkloadAndFanoutPair()
    {
        var directory = Path.Combine(Path.GetTempPath(), "musoq-scalar-reuse-family-gate", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var reports = Enumerable.Range(1, 3)
                .Select(index => WriteFamilyReport(directory, index))
                .ToArray();

            var result = StabilityAwareScalarReuseFamilyQualificationGate.Evaluate(reports);

            Assert.IsTrue(result.IsSuccess);
            Assert.HasCount(120, result.Comparisons);
            Assert.IsEmpty(result.Failures);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static string WriteFamilyReport(string directory, int index)
    {
        var benchmarks = new List<object>();
        foreach (var family in Enum.GetNames<ScalarReuseFamily>())
        foreach (var workload in Enum.GetNames<ScalarReuseWorkload>())
        foreach (var fanout in new[] { 1, 8, 64 })
        foreach (var enabled in new[] { false, true })
        {
            benchmarks.Add(new
            {
                FullName = $"Execute{(enabled ? "On" : "Off")}(Family: {family}, Workload: {workload}, Fanout: {fanout})",
                Statistics = new { Mean = enabled ? 95d : 100d },
                Memory = new { BytesAllocatedPerOperation = 10d }
            });
        }

        var path = Path.Combine(directory, $"cohort-{index}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new { Benchmarks = benchmarks }));
        return path;
    }
}
