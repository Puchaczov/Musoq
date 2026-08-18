using System.Globalization;

namespace Musoq.Benchmarks.Performance;

internal sealed record QueryRowQualificationInputs(
    IReadOnlyList<string> SourceReports,
    IReadOnlyList<string> CompiledReports,
    string DisassemblyPath);

internal sealed record QueryRowQualificationCheck(string Name, bool Passed, string Detail);

internal sealed record QueryRowQualificationResult(IReadOnlyList<QueryRowQualificationCheck> Checks)
{
    public bool IsSuccess => Checks.All(static check => check.Passed);
}

internal static class QueryRowQualificationGate
{
    public const int MinimumSamples = 3;
    private const int MaterializedRows = 2048;
    private const string SourceBenchmark = nameof(QueryScopedSourceMaterializationBenchmark);
    private const string CompiledBenchmark = nameof(QueryScopedCompiledExecutionBenchmark);

    private static readonly int[] FieldCounts = [2, 8, 32, 64];
    private static readonly string[][] SourceMethodGroups =
    [
        [nameof(QueryScopedSourceMaterializationBenchmark.LegacyRows), nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedStructRows), nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedClassRows)],
        [nameof(QueryScopedSourceMaterializationBenchmark.LegacySelectiveProjection), nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedSelectiveProjection), nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedClassSelectiveProjection)],
        [nameof(QueryScopedSourceMaterializationBenchmark.LegacyHighRejection), nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedHighRejection), nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedClassHighRejection)],
        [nameof(QueryScopedSourceMaterializationBenchmark.LegacyAggregation), nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedStructAggregation), nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedClassAggregation)],
        [nameof(QueryScopedSourceMaterializationBenchmark.LegacyEarlyTake), nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedEarlyTake), nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedClassEarlyTake)],
        [nameof(QueryScopedSourceMaterializationBenchmark.LegacyNumericRows), nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedNumericStructRows), nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedNumericClassRows)],
        [nameof(QueryScopedSourceMaterializationBenchmark.LegacyObjectArrayMaterialization), nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedStructMaterialization), nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedClassMaterialization)],
        [nameof(QueryScopedSourceMaterializationBenchmark.LegacyNumericObjectArrayMaterialization), nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedNumericStructMaterialization), nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedNumericClassMaterialization)]
    ];

    private static readonly string[] CompiledMethods =
    [
        nameof(QueryScopedCompiledExecutionBenchmark.LegacyWarmExecution),
        nameof(QueryScopedCompiledExecutionBenchmark.QueryScopedWarmExecution),
        nameof(QueryScopedCompiledExecutionBenchmark.LegacyColdCompileAndFirstRun),
        nameof(QueryScopedCompiledExecutionBenchmark.QueryScopedColdCompileAndFirstRun)
    ];

    public static QueryRowQualificationResult Evaluate(QueryRowQualificationInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ValidateReports(nameof(inputs.SourceReports), inputs.SourceReports);
        ValidateReports(nameof(inputs.CompiledReports), inputs.CompiledReports);

        var sourceReports = ReadCohort("source", inputs.SourceReports);
        var compiledReports = ReadCohort("compiled", inputs.CompiledReports);
        ValidateMatrix(sourceReports, compiledReports);

        var checks = new List<QueryRowQualificationCheck>();
        foreach (var fieldCount in FieldCounts)
        {
            var legacyCarrier = Median(
                sourceReports,
                SourceName(nameof(QueryScopedSourceMaterializationBenchmark.LegacyNumericObjectArrayMaterialization), fieldCount));
            var structCarrier = Median(
                sourceReports,
                SourceName(nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedNumericStructMaterialization), fieldCount));
            var classCarrier = Median(
                sourceReports,
                SourceName(nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedNumericClassMaterialization), fieldCount));
            var throughput = Ratio(legacyCarrier.MeanNanoseconds, structCarrier.MeanNanoseconds);
            var allocationReduction = Reduction(legacyCarrier.AllocatedBytes, structCarrier.AllocatedBytes);
            var maximumClassBytes = MaterializedRows * AlignToEight(24 + fieldCount * sizeof(int));

            checks.Add(Check(
                $"carrier-throughput-{fieldCount}",
                throughput >= 2d,
                $"{Format(throughput)}x legacy throughput; required >= 2.0000x"));
            checks.Add(Check(
                $"carrier-allocation-reduction-{fieldCount}",
                allocationReduction >= 0.9d,
                $"{FormatPercent(allocationReduction)} reduction; required >= 90.00%"));
            checks.Add(Check(
                $"struct-carrier-allocation-{fieldCount}",
                structCarrier.AllocatedBytes == 0d,
                $"{FormatBytes(structCarrier.AllocatedBytes)} allocated; required 0 B"));
            checks.Add(Check(
                $"class-carrier-allocation-{fieldCount}",
                classCarrier.AllocatedBytes <= maximumClassBytes,
                $"{FormatBytes(classCarrier.AllocatedBytes)} allocated; one-carrier ceiling {maximumClassBytes} B"));

            var legacyCsv = Median(
                sourceReports,
                SourceName(nameof(QueryScopedSourceMaterializationBenchmark.LegacyNumericRows), fieldCount));
            var structCsv = Median(
                sourceReports,
                SourceName(nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedNumericStructRows), fieldCount));
            var csvReduction = Reduction(legacyCsv.AllocatedBytes, structCsv.AllocatedBytes);
            checks.Add(Check(
                $"numeric-csv-allocation-{fieldCount}",
                csvReduction >= 0.2d,
                $"{FormatPercent(csvReduction)} reduction; required >= 20.00%"));
        }

        foreach (var scenario in Enum.GetValues<QueryRowCompiledScenario>())
        {
            var legacy = Median(
                compiledReports,
                CompiledName(nameof(QueryScopedCompiledExecutionBenchmark.LegacyWarmExecution), scenario));
            var queryScoped = Median(
                compiledReports,
                CompiledName(nameof(QueryScopedCompiledExecutionBenchmark.QueryScopedWarmExecution), scenario));
            var ratio = Ratio(queryScoped.MeanNanoseconds, legacy.MeanNanoseconds);
            var maximum = scenario is QueryRowCompiledScenario.NullableString8Full or
                QueryRowCompiledScenario.NullableString8HighRejection
                ? 1.05d
                : 1.03d;
            checks.Add(Check(
                $"warm-regression-{scenario}",
                ratio <= maximum,
                $"{Format(ratio)}x legacy time; required <= {Format(maximum)}x"));
        }

        checks.Add(EvaluateDisassembly(inputs.DisassemblyPath));
        return new QueryRowQualificationResult(checks.AsReadOnly());
    }

    private static QueryRowQualificationCheck EvaluateDisassembly(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A query-row disassembly path is required.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException("Query-row disassembly was not found.", path);

        var lines = File.ReadAllLines(path);
        var start = Array.FindIndex(
            lines,
            static line =>
                line.Contains("Assembly listing for method", StringComparison.OrdinalIgnoreCase) &&
                line.Contains("MaterializeNumericRows", StringComparison.Ordinal) &&
                line.Contains("BenchmarkNumericRow8", StringComparison.Ordinal));
        if (start < 0)
        {
            throw new InvalidDataException(
                "Query-row disassembly does not contain the warmed 8-field concrete numeric struct wrapper.");
        }

        var end = Array.FindIndex(
            lines,
            start + 1,
            static line => line.Contains("Assembly listing for method", StringComparison.OrdinalIgnoreCase));
        if (end < 0)
            end = lines.Length;

        var forbidden = lines[start..end]
            .Where(static line =>
                line.Contains("CORINFO_HELP_BOX", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("callvirt", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("interface dispatch", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("VIRTUAL_FUNC_PTR", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return Check(
            "warm-struct-disassembly",
            forbidden.Length == 0,
            forbidden.Length == 0
                ? "no boxing or interface/virtual dispatch markers"
                : $"forbidden markers: {string.Join(" | ", forbidden.Select(static line => line.Trim()))}");
    }

    private static void ValidateMatrix(
        IReadOnlyList<IReadOnlyDictionary<string, BenchmarkMetric>> sourceReports,
        IReadOnlyList<IReadOnlyDictionary<string, BenchmarkMetric>> compiledReports)
    {
        foreach (var fieldCount in FieldCounts)
        {
            foreach (var group in SourceMethodGroups)
            {
                foreach (var method in group)
                    Require(sourceReports, SourceName(method, fieldCount));
            }
        }

        foreach (var scenario in Enum.GetValues<QueryRowCompiledScenario>())
        {
            foreach (var method in CompiledMethods)
                Require(compiledReports, CompiledName(method, scenario));
        }
    }

    private static IReadOnlyDictionary<string, BenchmarkMetric>[] ReadCohort(
        string name,
        IReadOnlyList<string> paths)
    {
        var reports = paths.Select(BenchmarkReportReader.Read).ToArray();
        var expected = reports[0].Keys.ToHashSet(StringComparer.Ordinal);
        for (var index = 1; index < reports.Length; index++)
        {
            var missing = expected.Except(reports[index].Keys, StringComparer.Ordinal).Order().ToArray();
            var extra = reports[index].Keys.Except(expected, StringComparer.Ordinal).Order().ToArray();
            if (missing.Length == 0 && extra.Length == 0)
                continue;

            throw new InvalidDataException(
                $"The query-row {name} reports have different scenario sets at sample {index + 1}. " +
                $"Missing: {FormatNames(missing)}. Extra: {FormatNames(extra)}.");
        }

        return reports;
    }

    private static void Require(
        IReadOnlyList<IReadOnlyDictionary<string, BenchmarkMetric>> reports,
        string name)
    {
        if (reports.All(report => report.ContainsKey(name)))
            return;

        throw new InvalidDataException($"Query-row qualification report is missing scenario '{name}'.");
    }

    private static BenchmarkMetric Median(
        IReadOnlyList<IReadOnlyDictionary<string, BenchmarkMetric>> reports,
        string name)
    {
        return new BenchmarkMetric(
            Median(reports.Select(report => report[name].MeanNanoseconds)),
            Median(reports.Select(report => report[name].AllocatedBytes)));
    }

    private static double Median(IEnumerable<double> source)
    {
        var values = source.Order().ToArray();
        var midpoint = values.Length / 2;
        return values.Length % 2 == 1
            ? values[midpoint]
            : (values[midpoint - 1] + values[midpoint]) / 2d;
    }

    private static void ValidateReports(string name, IReadOnlyList<string> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);
        if (reports.Count < MinimumSamples)
        {
            throw new ArgumentException(
                $"At least {MinimumSamples} query-row benchmark reports are required.",
                name);
        }
    }

    private static string SourceName(string method, int fieldCount) =>
        $"Musoq.Benchmarks.{SourceBenchmark}.{method}(FieldCount: {fieldCount})";

    private static string CompiledName(string method, QueryRowCompiledScenario scenario) =>
        $"Musoq.Benchmarks.{CompiledBenchmark}.{method}(Scenario: {scenario})";

    private static QueryRowQualificationCheck Check(string name, bool passed, string detail) =>
        new(name, passed, detail);

    private static int AlignToEight(int value) => (value + 7) & ~7;

    private static double Ratio(double value, double baseline) =>
        baseline == 0d ? value == 0d ? 1d : double.PositiveInfinity : value / baseline;

    private static double Reduction(double baseline, double value) =>
        baseline == 0d ? value == 0d ? 1d : double.NegativeInfinity : 1d - value / baseline;

    private static string Format(double value) => value.ToString("F4", CultureInfo.InvariantCulture);

    private static string FormatPercent(double value) => value.ToString("P2", CultureInfo.InvariantCulture);

    private static string FormatBytes(double value) => value.ToString("F0", CultureInfo.InvariantCulture) + " B";

    private static string FormatNames(IReadOnlyCollection<string> names) =>
        names.Count == 0 ? "none" : string.Join(", ", names);
}
