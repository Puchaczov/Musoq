using System.Text.Json;
using Musoq.Benchmarks.Performance;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class QueryRowQualificationGateTests
{
    private static readonly int[] FieldCounts = [2, 8, 32, 64];
    private string _directory = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(
            Path.GetTempPath(),
            "musoq-query-row-gate",
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
    public void Evaluate_WhenThreeCompleteSamplesPassEveryThreshold_ShouldSucceedAndUseMedians()
    {
        var inputs = CreateInputs();

        var result = QueryRowQualificationGate.Evaluate(inputs);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Checks.All(static check => check.Passed));
        Assert.AreEqual(
            "2.3333x legacy throughput; required >= 2.0000x",
            result.Checks.Single(check => check.Name == "carrier-throughput-2").Detail);
    }

    [TestMethod]
    public void Evaluate_WhenRequiredScenarioIsAbsentFromEverySample_ShouldReportItsExactName()
    {
        var source = CreateSourceMetrics();
        var missing = SourceName(nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedClassEarlyTake), 64);
        source.Remove(missing);
        var sourceReports = WriteCohort("source-missing", source);
        var compiledReports = WriteCohort("compiled", CreateCompiledMetrics());
        var disassembly = WriteDisassembly(forbidden: false);

        var exception = Assert.Throws<InvalidDataException>(() =>
            QueryRowQualificationGate.Evaluate(new QueryRowQualificationInputs(
                sourceReports,
                compiledReports,
                disassembly)));

        StringAssert.Contains(exception.Message, missing);
    }

    [TestMethod]
    public void Evaluate_WhenSampleScenarioSetsDiffer_ShouldRejectTheCohortDeterministically()
    {
        var source = CreateSourceMetrics();
        var sourceReports = WriteCohort("source", source).ToArray();
        var incomplete = new Dictionary<string, BenchmarkMetric>(source, StringComparer.Ordinal);
        incomplete.Remove(SourceName(nameof(QueryScopedSourceMaterializationBenchmark.LegacyRows), 2));
        sourceReports[1] = WriteReport("source-incomplete", incomplete);

        var exception = Assert.Throws<InvalidDataException>(() =>
            QueryRowQualificationGate.Evaluate(new QueryRowQualificationInputs(
                sourceReports,
                WriteCohort("compiled", CreateCompiledMetrics()),
                WriteDisassembly(forbidden: false))));

        StringAssert.Contains(exception.Message, "different scenario sets at sample 2");
    }

    [TestMethod]
    public void Evaluate_WhenThresholdsFail_ShouldReturnNamedFailures()
    {
        var source = CreateSourceMetrics();
        source[SourceName(nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedNumericStructMaterialization), 8)] =
            new BenchmarkMetric(120d, 2_000d);
        source[SourceName(nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedNumericStructRows), 32)] =
            new BenchmarkMetric(100d, 9_000d);
        var compiled = CreateCompiledMetrics();
        compiled[CompiledName(
            nameof(QueryScopedCompiledExecutionBenchmark.QueryScopedWarmExecution),
            QueryRowCompiledScenario.NullableString8HighRejection)] = new BenchmarkMetric(106d, 100d);

        var result = QueryRowQualificationGate.Evaluate(new QueryRowQualificationInputs(
            WriteCohort("source", source),
            WriteCohort("compiled", compiled),
            WriteDisassembly(forbidden: true)));

        Assert.IsFalse(result.IsSuccess);
        var failures = result.Checks.Where(static check => !check.Passed).Select(static check => check.Name).ToArray();
        CollectionAssert.Contains(failures, "carrier-throughput-8");
        CollectionAssert.Contains(failures, "carrier-allocation-reduction-8");
        CollectionAssert.Contains(failures, "numeric-csv-allocation-32");
        CollectionAssert.Contains(failures, "warm-regression-NullableString8HighRejection");
        CollectionAssert.Contains(failures, "warm-struct-disassembly");
    }

    [TestMethod]
    public void Evaluate_WhenFewerThanThreeSamplesAreProvided_ShouldRejectInputs()
    {
        var source = CreateSourceMetrics();
        var compiled = CreateCompiledMetrics();

        var exception = Assert.Throws<ArgumentException>(() =>
            QueryRowQualificationGate.Evaluate(new QueryRowQualificationInputs(
                WriteCohort("source", source).Take(2).ToArray(),
                WriteCohort("compiled", compiled),
                WriteDisassembly(forbidden: false))));

        StringAssert.Contains(exception.Message, "At least 3 query-row benchmark reports");
    }

    [TestMethod]
    public void Command_WhenRequiredOptionIsMissing_ShouldReturnUsageError()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = QueryRowQualificationGateCommand.Run([], output, error);

        Assert.AreEqual(2, exitCode);
        StringAssert.Contains(error.ToString(), "--disassembly is required");
        StringAssert.Contains(error.ToString(), "Usage: gate-query-rows");
    }

    private QueryRowQualificationInputs CreateInputs()
    {
        var sourceSamples = new[]
        {
            CreateSourceMetrics(legacyCarrierTime: 210d, structCarrierTime: 100d),
            CreateSourceMetrics(legacyCarrierTime: 5_000d, structCarrierTime: 5d),
            CreateSourceMetrics(legacyCarrierTime: 200d, structCarrierTime: 90d)
        };
        return new QueryRowQualificationInputs(
            sourceSamples.Select((sample, index) => WriteReport($"source-{index}", sample)).ToArray(),
            WriteCohort("compiled", CreateCompiledMetrics()),
            WriteDisassembly(forbidden: false));
    }

    private static Dictionary<string, BenchmarkMetric> CreateSourceMetrics(
        double legacyCarrierTime = 210d,
        double structCarrierTime = 100d)
    {
        var metrics = new Dictionary<string, BenchmarkMetric>(StringComparer.Ordinal);
        foreach (var fieldCount in FieldCounts)
        {
            foreach (var method in SourceMethods())
                metrics[SourceName(method, fieldCount)] = new BenchmarkMetric(100d, 100d);

            metrics[SourceName(
                nameof(QueryScopedSourceMaterializationBenchmark.LegacyNumericObjectArrayMaterialization),
                fieldCount)] = new BenchmarkMetric(legacyCarrierTime, 10_000d);
            metrics[SourceName(
                nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedNumericStructMaterialization),
                fieldCount)] = new BenchmarkMetric(structCarrierTime, 0d);
            metrics[SourceName(
                nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedNumericClassMaterialization),
                fieldCount)] = new BenchmarkMetric(110d, 10_000d);
            metrics[SourceName(
                nameof(QueryScopedSourceMaterializationBenchmark.LegacyNumericRows),
                fieldCount)] = new BenchmarkMetric(100d, 10_000d);
            metrics[SourceName(
                nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedNumericStructRows),
                fieldCount)] = new BenchmarkMetric(90d, 7_000d);
        }

        return metrics;
    }

    private static Dictionary<string, BenchmarkMetric> CreateCompiledMetrics()
    {
        var metrics = new Dictionary<string, BenchmarkMetric>(StringComparer.Ordinal);
        foreach (var scenario in Enum.GetValues<QueryRowCompiledScenario>())
        {
            metrics[CompiledName(
                nameof(QueryScopedCompiledExecutionBenchmark.LegacyWarmExecution),
                scenario)] = new BenchmarkMetric(100d, 100d);
            metrics[CompiledName(
                nameof(QueryScopedCompiledExecutionBenchmark.QueryScopedWarmExecution),
                scenario)] = new BenchmarkMetric(102d, 100d);
            metrics[CompiledName(
                nameof(QueryScopedCompiledExecutionBenchmark.LegacyColdCompileAndFirstRun),
                scenario)] = new BenchmarkMetric(1_000d, 1_000d);
            metrics[CompiledName(
                nameof(QueryScopedCompiledExecutionBenchmark.QueryScopedColdCompileAndFirstRun),
                scenario)] = new BenchmarkMetric(1_000d, 1_000d);
        }

        return metrics;
    }

    private string[] WriteCohort(string prefix, Dictionary<string, BenchmarkMetric> metrics)
    {
        return Enumerable.Range(1, QueryRowQualificationGate.MinimumSamples)
            .Select(index => WriteReport($"{prefix}-{index}", metrics))
            .ToArray();
    }

    private string WriteReport(string name, IReadOnlyDictionary<string, BenchmarkMetric> metrics)
    {
        var path = Path.Combine(_directory, $"{name}.json");
        var report = new
        {
            Benchmarks = metrics.Select(metric => new
            {
                Method = metric.Key,
                FullName = metric.Key,
                Statistics = new { Mean = metric.Value.MeanNanoseconds },
                Memory = new { BytesAllocatedPerOperation = metric.Value.AllocatedBytes }
            })
        };
        File.WriteAllText(path, JsonSerializer.Serialize(report));
        return path;
    }

    private string WriteDisassembly(bool forbidden)
    {
        var path = Path.Combine(_directory, forbidden ? "forbidden.asm" : "clean.asm");
        File.WriteAllText(
            path,
            "; Assembly listing for method Musoq.Benchmarks.QueryScopedSourceMaterializationBenchmark:" +
            "MaterializeNumericRows[BenchmarkNumericRow8,BenchmarkNumericMaterializer8](int[]):int\n" +
            (forbidden ? "       call     CORINFO_HELP_BOX\n" : "       add      eax, edx\n") +
            "; Total bytes of code 42\n");
        return path;
    }

    private static IEnumerable<string> SourceMethods()
    {
        yield return nameof(QueryScopedSourceMaterializationBenchmark.LegacyRows);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedStructRows);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedClassRows);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.LegacySelectiveProjection);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedSelectiveProjection);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedClassSelectiveProjection);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.LegacyHighRejection);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedHighRejection);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedClassHighRejection);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.LegacyAggregation);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedStructAggregation);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedClassAggregation);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.LegacyEarlyTake);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedEarlyTake);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedClassEarlyTake);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.LegacyNumericRows);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedNumericStructRows);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedNumericClassRows);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.LegacyObjectArrayMaterialization);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedStructMaterialization);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedClassMaterialization);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.LegacyNumericObjectArrayMaterialization);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedNumericStructMaterialization);
        yield return nameof(QueryScopedSourceMaterializationBenchmark.QueryScopedNumericClassMaterialization);
    }

    private static string SourceName(string method, int fieldCount) =>
        $"Musoq.Benchmarks.{nameof(QueryScopedSourceMaterializationBenchmark)}.{method}(FieldCount: {fieldCount})";

    private static string CompiledName(string method, QueryRowCompiledScenario scenario) =>
        $"Musoq.Benchmarks.{nameof(QueryScopedCompiledExecutionBenchmark)}.{method}(Scenario: {scenario})";
}
