using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class BenchmarkRegressionComparisonTests
{
    private static readonly Measurement Baseline = CampaignTests.Success() with
    {
        ConfidenceLowerNanoseconds = 8,
        ConfidenceUpperNanoseconds = 12,
        AllocatedBytesPerOperation = 0,
        RawReports = ["raw/benchmark-report-full.json", "raw/measured-process-fixture.json"],
    };

    [TestMethod]
    public void ComparisonReportsDeltasAndIntervalsWithoutConcludingARegression()
    {
        var candidate = Baseline with
        {
            MeanNanoseconds = 15,
            ConfidenceLowerNanoseconds = 9,
            ConfidenceUpperNanoseconds = 21,
            AllocatedBytesPerOperation = 16,
            SampleCount = 5,
        };
        var result = BenchmarkRegressionComparison.CompareMeasurements(Baseline, candidate, "execution", 2);

        Assert.AreEqual("comparable", result.Status);
        Assert.AreEqual(2, result.Pair);
        Assert.AreEqual(5, result.Timing!.DeltaNanoseconds);
        Assert.AreEqual(50, result.Timing.ChangePercent);
        Assert.AreEqual("overlap", result.Timing.ConfidenceIntervalRelation);
        Assert.AreEqual(5, result.Timing.Candidate.SampleCount);
        Assert.AreEqual(16L, result.Allocations!.DeltaBytes);
        Assert.IsNull(result.Allocations.ChangePercent);
    }

    [TestMethod]
    public void ColdCompilationCanCompareTimingWithUnavailableAllocations()
    {
        var measurement = Baseline with { AllocatedBytesPerOperation = null };

        var result = BenchmarkRegressionComparison.CompareMeasurements(measurement, measurement, "compilation-cold", 1);

        Assert.AreEqual("comparable", result.Status);
        Assert.IsNull(result.Allocations);
        Assert.AreEqual("First compilation does not collect allocations.", result.AllocationUnavailableReason);
    }

    [TestMethod]
    public void MissingNativeEvidenceIsUnavailable()
    {
        var candidate = Baseline with { RawReports = [] };

        var result = BenchmarkRegressionComparison.CompareMeasurements(Baseline, candidate, "execution", 1);

        Assert.AreEqual("unavailable", result.Status);
        Assert.IsNull(result.Timing);
        StringAssert.Contains(result.Reason!, "native timing");
    }

    [TestMethod]
    public void SelectedCaseWithoutARunIsReportedAsUnavailable()
    {
        using var directory = new TemporaryDirectory();
        var source = new SourceStamp("candidate", null, [], false, null);
        ArtifactJson.Write(
            Path.Combine(directory.Path, "changes.json"),
            new BenchmarkSnapshotDiff("suite", "baseline", source, true, [], [])
        );
        var input = Path.Combine(directory.Path, "comparison.json");
        ArtifactJson.Write(input, new BenchmarkComparisonInput("changes.json", [], ["selected"]));

        var result = BenchmarkRegressionComparison.Read(input).Cases.Single();

        Assert.AreEqual("selected", result.CaseId);
        Assert.AreEqual("unavailable", result.Status);
        StringAssert.Contains(result.Reason!, "No supplied run pair");
    }
}
