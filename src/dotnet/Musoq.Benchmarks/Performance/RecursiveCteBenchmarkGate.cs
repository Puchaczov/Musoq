using Musoq.Evaluator;

namespace Musoq.Benchmarks.Performance;

internal static class RecursiveCteBenchmarkGate
{
    public const int RequiredSamples = 3;
    public const double MaximumTimeRatio = 1.25d;
    public const double MaximumAllocationRatio = 1.20d;
    public const double MaximumRegressionRatio = 1.03d;

    private static readonly RecursiveCteBenchmarkScenario[] HandwrittenEquivalentScenarios =
    [
        RecursiveCteBenchmarkScenario.Chain,
        RecursiveCteBenchmarkScenario.Tree,
        RecursiveCteBenchmarkScenario.Diamond,
        RecursiveCteBenchmarkScenario.Cycle,
        RecursiveCteBenchmarkScenario.DuplicateHeavyKeyed,
        RecursiveCteBenchmarkScenario.WideRows,
        RecursiveCteBenchmarkScenario.InvariantSnapshot,
        RecursiveCteBenchmarkScenario.IndexedEdges
    ];

    public static BenchmarkComparisonResult Compare(
        IReadOnlyList<string> reportPaths,
        double maximumTimeRatio = MaximumTimeRatio,
        double maximumAllocationRatio = MaximumAllocationRatio)
    {
        ValidateReports(nameof(reportPaths), reportPaths);
        ValidateRatio(nameof(maximumTimeRatio), maximumTimeRatio);
        ValidateRatio(nameof(maximumAllocationRatio), maximumAllocationRatio);

        var reports = reportPaths.Select(BenchmarkReportReader.Read).ToArray();
        var comparisons = new List<BenchmarkComparison>(HandwrittenEquivalentScenarios.Length);
        foreach (var scenario in HandwrittenEquivalentScenarios)
        {
            var baseline = Median(
                reports,
                scenario,
                ParallelizationMode.None,
                nameof(RecursiveCteBenchmark.HandwrittenSemiNaive));
            var current = Median(
                reports,
                scenario,
                ParallelizationMode.None,
                nameof(RecursiveCteBenchmark.MusoqGenerated));
            var timeRatio = Ratio(current.MeanNanoseconds, baseline.MeanNanoseconds);
            var allocationRatio = Ratio(current.AllocatedBytes, baseline.AllocatedBytes);
            comparisons.Add(new BenchmarkComparison(
                $"Sequential/{scenario}",
                baseline,
                current,
                timeRatio,
                allocationRatio,
                timeRatio > maximumTimeRatio || allocationRatio > maximumAllocationRatio));
        }

        return new BenchmarkComparisonResult(comparisons);
    }

    public static RecursiveCtePerformanceGateResult CompareTiered(RecursiveCtePerformanceGateInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ValidateReports(nameof(inputs.BaselineRuntimeReports), inputs.BaselineRuntimeReports);
        ValidateReports(nameof(inputs.CurrentRuntimeReports), inputs.CurrentRuntimeReports);
        ValidateReports(nameof(inputs.BaselineCompilationReports), inputs.BaselineCompilationReports);
        ValidateReports(nameof(inputs.CurrentCompilationReports), inputs.CurrentCompilationReports);
        ValidateReports(nameof(inputs.BaselineOrdinaryCteReports), inputs.BaselineOrdinaryCteReports);
        ValidateReports(nameof(inputs.CurrentOrdinaryCteReports), inputs.CurrentOrdinaryCteReports);

        return new RecursiveCtePerformanceGateResult(
        [
            new RecursiveCtePerformanceTier(
                "sequential-equivalence",
                MaximumTimeRatio,
                MaximumAllocationRatio,
                Compare(inputs.CurrentRuntimeReports)),
            CreateRegressionTier(
                "full-mode-regression",
                inputs.BaselineRuntimeReports,
                inputs.CurrentRuntimeReports,
                name => IsRuntimeMethod(name, HandwrittenEquivalentScenarios, ParallelizationMode.Full)),
            CreateRegressionTier(
                "correlated-apply-overhead",
                inputs.BaselineRuntimeReports,
                inputs.CurrentRuntimeReports,
                name => IsRuntimeMethod(
                    name,
                    [RecursiveCteBenchmarkScenario.CorrelatedApply],
                    ParallelizationMode.Full)),
            CreateRegressionTier(
                "empty-anchor-overhead",
                inputs.BaselineRuntimeReports,
                inputs.CurrentRuntimeReports,
                name => IsRuntimeMethod(
                    name,
                    [RecursiveCteBenchmarkScenario.EmptyAnchor],
                    ParallelizationMode.Full)),
            CreateRegressionTier(
                "recursive-compilation-regression",
                inputs.BaselineCompilationReports,
                inputs.CurrentCompilationReports,
                name => IsBenchmarkMethod(name, nameof(RecursiveCteCompilationBenchmark), nameof(RecursiveCteCompilationBenchmark.Compile))),
            CreateRegressionTier(
                "ordinary-cte-regression",
                inputs.BaselineOrdinaryCteReports,
                inputs.CurrentOrdinaryCteReports,
                name => name.Contains($".{nameof(CteSidecarIndexBenchmark)}.", StringComparison.Ordinal))
        ]);
    }

    private static RecursiveCtePerformanceTier CreateRegressionTier(
        string name,
        IReadOnlyList<string> baselineReports,
        IReadOnlyList<string> currentReports,
        Func<string, bool> methodFilter) =>
        new(
            name,
            MaximumRegressionRatio,
            MaximumRegressionRatio,
            BenchmarkReportComparer.Compare(
                baselineReports,
                currentReports,
                MaximumRegressionRatio,
                MaximumRegressionRatio,
                RequiredSamples,
                methodFilter));

    private static bool IsRuntimeMethod(
        string name,
        IReadOnlyCollection<RecursiveCteBenchmarkScenario> scenarios,
        ParallelizationMode mode) =>
        IsBenchmarkMethod(name, nameof(RecursiveCteBenchmark), nameof(RecursiveCteBenchmark.MusoqGenerated)) &&
        scenarios.Any(scenario => HasParameter(name, nameof(RecursiveCteBenchmark.Scenario), scenario)) &&
        HasParameter(name, nameof(RecursiveCteBenchmark.ExecutionMode), mode);

    private static bool IsBenchmarkMethod(string name, string benchmark, string method) =>
        name.Contains($".{benchmark}.{method}(", StringComparison.Ordinal);

    private static bool HasParameter<T>(string name, string parameter, T value) =>
        name.Contains($"{parameter}: {value}", StringComparison.Ordinal);

    private static BenchmarkMetric Median(
        IEnumerable<IReadOnlyDictionary<string, BenchmarkMetric>> reports,
        RecursiveCteBenchmarkScenario scenario,
        ParallelizationMode mode,
        string method)
    {
        var metrics = reports.Select(report => Find(report, scenario, mode, method)).ToArray();
        return new BenchmarkMetric(
            Median(metrics.Select(static metric => metric.MeanNanoseconds)),
            Median(metrics.Select(static metric => metric.AllocatedBytes)));
    }

    private static BenchmarkMetric Find(
        IReadOnlyDictionary<string, BenchmarkMetric> report,
        RecursiveCteBenchmarkScenario scenario,
        ParallelizationMode mode,
        string method)
    {
        var matches = report
            .Where(entry =>
                IsBenchmarkMethod(entry.Key, nameof(RecursiveCteBenchmark), method) &&
                HasParameter(entry.Key, nameof(RecursiveCteBenchmark.Scenario), scenario) &&
                HasParameter(entry.Key, nameof(RecursiveCteBenchmark.ExecutionMode), mode))
            .Select(static entry => entry.Value)
            .ToArray();
        return matches.Length == 1
            ? matches[0]
            : throw new InvalidDataException(
                $"Expected one {method} result for recursive scenario {scenario} in {mode} mode, found {matches.Length}.");
    }

    private static void ValidateReports(string name, IReadOnlyList<string> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);
        if (reports.Count != RequiredSamples)
            throw new ArgumentException($"Exactly {RequiredSamples} recursive benchmark reports are required.", name);
    }

    private static void ValidateRatio(string name, double value)
    {
        if (!double.IsFinite(value) || value < 1)
            throw new ArgumentOutOfRangeException(name);
    }

    private static double Median(IEnumerable<double> source)
    {
        var values = source.Order().ToArray();
        return values[values.Length / 2];
    }

    private static double Ratio(double current, double baseline) =>
        baseline == 0 ? current == 0 ? 1d : double.PositiveInfinity : current / baseline;
}
