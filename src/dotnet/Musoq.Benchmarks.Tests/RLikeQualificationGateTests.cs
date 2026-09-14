using System.Text.Json;
using Musoq.Benchmarks.Performance;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class RLikeQualificationGateTests
{
    private string _directory = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "musoq-rlike-gate", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [TestMethod]
    public void Evaluate_WhenEveryComparisonIsWithinThreshold_ShouldSucceed()
    {
        var result = RLikeQualificationGate.Evaluate(CreateInputs(0.97d, 1.03d));

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(RLikeQualificationGate.ExpectedComparisonCount, result.Comparisons);
    }

    [TestMethod]
    public void Evaluate_WhenRuntimeComparisonExceedsThreshold_ShouldFail()
    {
        var inputs = CreateInputs(1d, 1d) with
        {
            Constant = CreateReports("constant-regression", ConstantMethods, 1.031d)
        };

        var result = RLikeQualificationGate.Evaluate(inputs);

        Assert.IsFalse(result.IsSuccess);
        Assert.HasCount(4, result.Comparisons.Where(static comparison => comparison.IsRegression).ToArray());
    }

    [TestMethod]
    public void Evaluate_WhenCompilationComparisonExceedsThreshold_ShouldFail()
    {
        var result = RLikeQualificationGate.Evaluate(CreateInputs(1d, 1.031d));

        Assert.IsFalse(result.IsSuccess);
        Assert.HasCount(2, result.CompilationComparisons.Where(static comparison => comparison.IsRegression).ToArray());
    }

    [TestMethod]
    public void Evaluate_WhenReportOmitsRequiredBenchmark_ShouldRejectIncompleteEvidence()
    {
        var inputs = CreateInputs(1d, 1d) with
        {
            Apply = CreateReports("apply-incomplete", ApplyMethods[..^1], 1d)
        };

        var exception = Assert.ThrowsExactly<InvalidDataException>(
            () => RLikeQualificationGate.Evaluate(inputs));

        Assert.Contains("exactly 6", exception.Message);
    }

    [TestMethod]
    public void Command_WhenOptionsAreIncomplete_ShouldReturnUsageError()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = RLikeQualificationGateCommand.Run([], output, error);

        Assert.AreEqual(2, exitCode);
        Assert.Contains("Usage: gate-rlike", error.ToString());
    }

    [TestMethod]
    public void Command_WhenReportIsIncomplete_ShouldReturnEvidenceError()
    {
        var inputs = CreateInputs(1d, 1d) with
        {
            Apply = CreateReports("command-apply-incomplete", ApplyMethods[..^1], 1d)
        };
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = RLikeQualificationGateCommand.Run(ToArguments(inputs), output, error);

        Assert.AreEqual(2, exitCode);
        Assert.Contains("exactly 6", error.ToString());
        Assert.Contains("Usage: gate-rlike", error.ToString());
    }

    [TestMethod]
    public void Command_WhenGateFails_ShouldPrintExactFailureWithoutCallingItAnImprovement()
    {
        var inputs = CreateInputs(1d, 1.031d);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = RLikeQualificationGateCommand.Run(ToArguments(inputs), output, error);

        Assert.AreEqual(1, exitCode);
        Assert.Contains("[FAILED]", output.ToString());
        Assert.IsTrue(
            output.ToString()
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Where(static line => line.StartsWith("compilation/", StringComparison.Ordinal))
                .All(static line => line.EndsWith("[FAILED]", StringComparison.Ordinal)));
        Assert.Contains("RLIKE qualification failed.", error.ToString());
    }

    private RLikeQualificationInputs CreateInputs(double runtimeRatio, double compilationRatio) => new(
        CreateReports("legacy", LegacyMethods, runtimeRatio),
        CreateReports("apply", ApplyMethods, runtimeRatio),
        CreateReports("compilation", CompilationMethods, compilationRatio),
        CreateReports("constant", ConstantMethods, runtimeRatio),
        CreateReports("dynamic", DynamicMethods, runtimeRatio),
        CreateReports("length", LengthMethods, runtimeRatio));

    private RLikeBenchmarkReports CreateReports(
        string name,
        IReadOnlyList<string> methods,
        double currentRatio) => new(
        WriteReport($"{name}-baseline", methods, 100d),
        WriteReport($"{name}-current", methods, 100d * currentRatio));

    private string WriteReport(string name, IReadOnlyList<string> methods, double mean)
    {
        var path = Path.Combine(_directory, $"{name}.json");
        var report = new
        {
            Benchmarks = methods.Select(method => new
            {
                FullName = method,
                Statistics = new { Mean = mean },
                Memory = new { BytesAllocatedPerOperation = mean }
            })
        };
        File.WriteAllText(path, JsonSerializer.Serialize(report));
        return path;
    }

    private static string[] ToArguments(RLikeQualificationInputs inputs) =>
    [
        "--legacy-baseline", inputs.Legacy.Baseline,
        "--legacy-current", inputs.Legacy.Current,
        "--apply-baseline", inputs.Apply.Baseline,
        "--apply-current", inputs.Apply.Current,
        "--compilation-baseline", inputs.Compilation.Baseline,
        "--compilation-current", inputs.Compilation.Current,
        "--constant-baseline", inputs.Constant.Baseline,
        "--constant-current", inputs.Constant.Current,
        "--dynamic-baseline", inputs.Dynamic.Baseline,
        "--dynamic-current", inputs.Dynamic.Current,
        "--length-baseline", inputs.InputLength.Baseline,
        "--length-current", inputs.InputLength.Current
    ];

    private static readonly string[] LegacyMethods =
    [
        "Musoq.Benchmarks.RegexOptimizationBenchmark.RLike_Pattern_1000Rows"
    ];

    private static readonly string[] ApplyMethods =
        new[] { 1, 8, 64 }
            .SelectMany(static fanOut => new[]
            {
                $"Musoq.Benchmarks.RLikeApplyBenchmark.RLikeApply_InnerPattern(FanOut: {fanOut})",
                $"Musoq.Benchmarks.RLikeApplyBenchmark.RLikeApply_OuterPattern(FanOut: {fanOut})"
            })
            .ToArray();

    private static readonly string[] CompilationMethods =
    [
        "Musoq.Benchmarks.RLikeCompilationBenchmark.DynamicRLike_Compilation",
        "Musoq.Benchmarks.RLikeCompilationBenchmark.RLikeApply_Compilation"
    ];

    private static readonly string[] ConstantMethods =
        Enum.GetNames<RLikeBenchmarkScenario>()
            .Select(static scenario =>
                $"Musoq.Benchmarks.RLikeConstantBenchmark.ConstantRLike_Run(Scenario: {scenario})")
            .ToArray();

    private static readonly string[] DynamicMethods =
        new[] { 1, 2, 64, 512, 4096 }
            .SelectMany(cardinality => Enum.GetNames<RLikeBenchmarkScenario>().Select(scenario =>
                $"Musoq.Benchmarks.RLikeExecutionBenchmark.DynamicRLike_Run(PatternCardinality: {cardinality}, Scenario: {scenario})"))
            .ToArray();

    private static readonly string[] LengthMethods =
        new[] { 4, 64, 4096 }
            .SelectMany(static length => new[]
            {
                $"Musoq.Benchmarks.RLikeInputLengthBenchmark.RLike_ByInputLength(InputLength: {length}, Scenario: Literal)",
                $"Musoq.Benchmarks.RLikeInputLengthBenchmark.RLike_ByInputLength(InputLength: {length}, Scenario: Complex)"
            })
            .ToArray();
}
