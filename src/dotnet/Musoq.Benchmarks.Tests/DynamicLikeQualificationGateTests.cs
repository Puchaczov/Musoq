using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks.Performance;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class DynamicLikeQualificationGateTests
{
    private string _directory = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "musoq-dynamic-like-gate", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [TestMethod]
    public void Evaluate_WhenEveryThresholdIsMet_ShouldSucceed()
    {
        var result = Evaluate(
            low: 90d,
            medium: 95d,
            high: 103d,
            length: 103d,
            innerApply: 90d,
            outerApply: 80d,
            unicode: 85d,
            ascii: 103d,
            compilation: 105d);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(19, result.Comparisons.ToArray());
    }

    [TestMethod]
    public void Evaluate_WhenLowCardinalityOrOuterApplyMissesThreshold_ShouldFail()
    {
        var result = Evaluate(
            low: 91d,
            medium: 95d,
            high: 103d,
            length: 103d,
            innerApply: 90d,
            outerApply: 81d,
            unicode: 85d,
            ascii: 103d,
            compilation: 105d);

        Assert.IsFalse(result.IsSuccess);
        Assert.HasCount(3, result.Comparisons.Where(static comparison => comparison.IsRegression).ToArray());
    }

    [TestMethod]
    public void Command_WhenReportCountsAreIncomplete_ShouldReturnUsageError()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = DynamicLikeQualificationGateCommand.Run([], output, error);

        Assert.AreEqual(2, exitCode);
        Assert.Contains("Usage: gate-dynamic-like", error.ToString());
    }

    private DynamicLikeQualificationResult Evaluate(
        double low,
        double medium,
        double high,
        double length,
        double innerApply,
        double outerApply,
        double unicode,
        double ascii,
        double compilation)
    {
        var dynamicBaseline = WriteDynamicCohort("dynamic-baseline", 100d, 100d, 100d);
        var dynamicCurrent = WriteDynamicCohort("dynamic-current", low, medium, high);
        var lengthBaseline = WriteLengthCohort("length-baseline", 100d);
        var lengthCurrent = WriteLengthCohort("length-current", length);
        var applyBaseline = WriteApplyCohort("apply-baseline", 100d, 100d);
        var applyCurrent = WriteApplyCohort("apply-current", innerApply, outerApply);
        var constantBaseline = WriteConstantCohort("constant-baseline", 100d, 100d);
        var constantCurrent = WriteConstantCohort("constant-current", unicode, ascii);
        var compilationBaseline = WriteCompilationCohort("compilation-baseline", 100d);
        var compilationCurrent = WriteCompilationCohort("compilation-current", compilation);

        return DynamicLikeQualificationGate.Evaluate(
            dynamicBaseline,
            dynamicCurrent,
            lengthBaseline,
            lengthCurrent,
            applyBaseline,
            applyCurrent,
            constantBaseline,
            constantCurrent,
            compilationBaseline,
            compilationCurrent);
    }

    private string[] WriteDynamicCohort(string prefix, double low, double medium, double high) =>
        WriteCohort(
            prefix,
            ("DynamicLike_Run(PatternCardinality: 1, Scenario: Ascii)", low),
            ("DynamicLike_Run(PatternCardinality: 2, Scenario: Unicode)", low),
            ("DynamicLike_Run(PatternCardinality: 64, Scenario: Wildcard)", medium),
            ("DynamicLike_Run(PatternCardinality: 512, Scenario: Ascii)", medium),
            ("DynamicLike_Run(PatternCardinality: 4096, Scenario: Unicode)", high));

    private string[] WriteLengthCohort(string prefix, double mean) =>
        WriteCohort(prefix, ("DynamicLike_ByInputLength(InputLength: 4096, Scenario: Unicode)", mean));

    private string[] WriteApplyCohort(string prefix, double inner, double outer) =>
        WriteCohort(
            prefix,
            ("DynamicLikeApply_InnerPattern(FanOut: 64)", inner),
            ("DynamicLikeApply_OuterPattern(FanOut: 64)", outer));

    private string[] WriteConstantCohort(string prefix, double unicode, double ascii) =>
        WriteCohort(
            prefix,
            ("Like_Fallback_SingleCharacterWildcard(RowsCount: 100000)", unicode),
            ("Like_Fallback_InteriorWildcard(RowsCount: 100000)", unicode),
            ("Like_Fallback_NonAsciiPattern(RowsCount: 100000)", unicode),
            ("Like_UnicodeInput(RowsCount: 100000)", unicode),
            ("Like_Exact(RowsCount: 100000)", ascii),
            ("Like_Prefix(RowsCount: 100000)", ascii),
            ("Like_Suffix(RowsCount: 100000)", ascii),
            ("Like_Contains(RowsCount: 100000)", ascii),
            ("Like_MultipleMatches(RowsCount: 100000)", ascii));

    private string[] WriteCompilationCohort(string prefix, double mean) =>
        WriteCohort(
            prefix,
            ("DynamicLike_Compilation", mean),
            ("DynamicLikeApply_Compilation", mean));

    private string[] WriteCohort(string prefix, params (string Method, double Mean)[] benchmarks)
    {
        return Enumerable.Range(1, DynamicLikeQualificationGate.RequiredReports)
            .Select(index => WriteReport($"{prefix}-{index}", benchmarks))
            .ToArray();
    }

    private string WriteReport(string name, IReadOnlyList<(string Method, double Mean)> benchmarks)
    {
        var path = Path.Combine(_directory, $"{name}.json");
        var report = new
        {
            Benchmarks = benchmarks.Select(benchmark => new
            {
                FullName = benchmark.Method,
                Statistics = new { benchmark.Mean },
                Memory = new { BytesAllocatedPerOperation = benchmark.Mean }
            })
        };
        File.WriteAllText(path, JsonSerializer.Serialize(report));
        return path;
    }
}
