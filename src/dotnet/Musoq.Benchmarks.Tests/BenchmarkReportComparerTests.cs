using Musoq.Benchmarks.Performance;
using System.Text.Json;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class BenchmarkReportComparerTests
{
    private string _directory = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "musoq-benchmark-comparer", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [TestMethod]
    public void Compare_UsesMedianAndAllowsCurrentOnlyMethods()
    {
        var baseline = new[]
        {
            WriteReport("baseline-1", ("Existing", 100d, 100d)),
            WriteReport("baseline-2", ("Existing", 1000d, 1000d)),
            WriteReport("baseline-3", ("Existing", 90d, 90d))
        };
        var current = new[]
        {
            WriteReport("current-1", ("Existing", 102d, 102d), ("New", 500d, 500d)),
            WriteReport("current-2", ("Existing", 101d, 101d), ("New", 510d, 510d)),
            WriteReport("current-3", ("Existing", 100d, 100d), ("New", 490d, 490d))
        };

        var result = BenchmarkReportComparer.Compare(baseline, current, 1.03d, 1.03d);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, result.Comparisons);
        Assert.AreEqual("Existing", result.Comparisons[0].Method);
        Assert.AreEqual(100d, result.Comparisons[0].Baseline.MeanNanoseconds);
        Assert.AreEqual(101d, result.Comparisons[0].Current.MeanNanoseconds);
    }

    [TestMethod]
    public void Compare_WhenTimingOrAllocationExceedsThreshold_ShouldFail()
    {
        var baseline = WriteCohort("baseline", 100d, 100d);
        var current = WriteCohort("current", 104d, 104d);

        var result = BenchmarkReportComparer.Compare(baseline, current, 1.03d, 1.03d);

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.Comparisons.Single().IsRegression);
    }

    [TestMethod]
    public void Compare_WhenZeroAllocationBaselineGainsAllocation_ShouldFail()
    {
        var baseline = WriteCohort("baseline", 100d, 0d);
        var current = WriteCohort("current", 100d, 1d);

        var result = BenchmarkReportComparer.Compare(baseline, current, 1.03d, 1.03d);

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(double.IsPositiveInfinity(result.Comparisons.Single().AllocationRatio));
    }

    [TestMethod]
    public void Read_WhenReportIsPartial_ShouldRejectIt()
    {
        var path = Path.Combine(_directory, "partial.json");
        File.WriteAllText(path, """{"Benchmarks":[{"Method":"Broken","Statistics":null,"Memory":{"BytesAllocatedPerOperation":0}}]}""");

        var exception = Assert.Throws<InvalidDataException>(() => BenchmarkReportReader.Read(path));

        StringAssert.Contains(exception.Message, "partial");
    }

    [TestMethod]
    public void Compare_WhenCohortMethodSetsDiffer_ShouldRejectIt()
    {
        var baseline = new[]
        {
            WriteReport("baseline-1", ("One", 100d, 100d)),
            WriteReport("baseline-2", ("One", 100d, 100d), ("Two", 100d, 100d)),
            WriteReport("baseline-3", ("One", 100d, 100d))
        };
        var current = WriteCohort("current", 100d, 100d);

        var exception = Assert.Throws<InvalidDataException>(() =>
            BenchmarkReportComparer.Compare(baseline, current, 1.03d, 1.03d));

        StringAssert.Contains(exception.Message, "different method sets");
    }

    private string[] WriteCohort(string prefix, double mean, double allocated)
    {
        return Enumerable.Range(1, 3)
            .Select(index => WriteReport($"{prefix}-{index}", ("One", mean, allocated)))
            .ToArray();
    }

    private string WriteReport(string name, params (string Method, double Mean, double Allocated)[] benchmarks)
    {
        var path = Path.Combine(_directory, $"{name}.json");
        var report = new
        {
            Benchmarks = benchmarks.Select(benchmark => new
            {
                benchmark.Method,
                Statistics = new { benchmark.Mean },
                Memory = new { BytesAllocatedPerOperation = benchmark.Allocated }
            })
        };
        File.WriteAllText(path, JsonSerializer.Serialize(report));
        return path;
    }
}
