using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks.Scenarios;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class CampaignTests
{
    [TestMethod]
    public void ExecutionSizes_UseThreeStandardCountsWithoutScenarioCaps()
    {
        var cases = CampaignCases.Create(new CampaignOptions(), Path.GetTempPath());
        var counts = cases.Where(item => item.Definition.Phase == "execution")
            .GroupBy(item => item.Definition.ScenarioId)
            .Select(group => string.Join(",", group.Select(item => item.Definition.InputCount).Distinct().Order())).Distinct().ToArray();
        CollectionAssert.AreEqual(new[] { "1000,10000,100000" }, counts);
    }

    [TestMethod]
    public void CustomSize_ReplacesExecutionMatrixAndLeavesLifecycleFixturesUnchanged()
    {
        var options = CampaignOptions.Parse(["--size", "1_007"]);
        var cases = CampaignCases.Create(options, Path.GetTempPath());
        Assert.AreEqual(69, cases.Count(item => item.Definition.Phase == "execution"));
        Assert.IsTrue(cases.Where(item => item.Definition.Phase == "execution").All(item => item.Definition.InputCount == 1007));
        Assert.IsTrue(cases.Where(item => item.Definition.Phase != "execution").All(item => item.Definition.InputCount == FixtureData.TestSize));
        Assert.Throws<ArgumentException>(() => CampaignOptions.Parse(["--size", "0"]));
        Assert.Throws<ArgumentException>(() => CampaignOptions.Parse(["--mode", "Official", "--machine-profile", "desktop", "--size", "7"]));
    }

    [TestMethod]
    public void ArtifactRoundTrip_PreservesOrdinalArgumentOrderingForResume()
    {
        using var temporary = new TemporaryDirectory();
        var path = System.IO.Path.Combine(temporary.Path, "arguments.json");
        var arguments = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["WorkerLimit"] = "24",
            ["InputCount"] = "1000"
        };
        ArtifactJson.Write(path, arguments);
        var reloaded = ArtifactJson.Read<SortedDictionary<string, string>>(path);
        Assert.AreEqual(StringComparer.Ordinal, reloaded.Comparer);
        CollectionAssert.AreEqual(arguments.ToArray(), reloaded.ToArray());
    }

    [TestMethod]
    public void MeasuredProcess_IdentifiesLoadedRuntimeAndRevisions()
    {
        var stamp = MeasuredProcess.Capture();
        Assert.AreEqual(Environment.ProcessId, stamp.ProcessId);
        Assert.AreEqual(Environment.ProcessorCount, stamp.EffectiveProcessors);
        Assert.AreEqual(System.Runtime.GCSettings.IsServerGC, stamp.ServerGc);
        Assert.IsNotEmpty(stamp.EngineInformationalVersion);
        Assert.IsNotEmpty(stamp.SuiteInformationalVersion);
        Assert.IsTrue(stamp.Runtime.StartsWith(".NET", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Options_SeparateOfficialFromLocalOverrides()
    {
        Assert.IsTrue(CampaignOptions.Parse(["--mode", "Official", "--machine-profile", "desktop"]).Official);
        Assert.AreEqual("Dry", CampaignOptions.Parse(["--job", "dry", "--filter", "*Join*"]).Job);
        Assert.Throws<ArgumentException>(() => CampaignOptions.Parse(["--mode", "Official"]));
        Assert.Throws<ArgumentException>(() => CampaignOptions.Parse(["--mode", "Official", "--machine-profile", "a", "--job", "Dry"]));
        Assert.Throws<ArgumentException>(() => CampaignOptions.Parse(["--job"]));
        Assert.Throws<ArgumentException>(() => CampaignOptions.Parse(["--unknown", "value"]));
        Assert.Throws<ArgumentException>(() => CampaignOptions.Parse(["--resume", "a", "--output", "b"]));
        Assert.Throws<ArgumentException>(() => CampaignOptions.Parse(["--in-process"]));
        Assert.Throws<ArgumentException>(() => CampaignOptions.Parse(["--list"]));
        Assert.AreEqual("List", CampaignOptions.Parse(["--action", "List"]).Action);
    }

    [TestMethod]
    public void Inventory_CoversAllFamiliesAndRegisteredBenchmarkTypes()
    {
        var families = ScenarioCatalog.All.Where(item => item.Id.StartsWith('B')).Select(item => int.Parse(item.Id.AsSpan(1, 2))).Distinct().Order().ToArray();
        CollectionAssert.AreEqual(Enumerable.Range(1, 32).ToArray(), families);
        Assert.AreEqual(10, ScenarioCatalog.All.Count(item => item.Id.StartsWith('X')));
        Type[] actual =
        [
            typeof(QueryExecutionBenchmark), typeof(ColdCompilationBenchmark), typeof(WarmCompilationBenchmark),
            typeof(ArtifactLoadBenchmark), typeof(TypedProjectionBenchmark), typeof(TypedGroupBenchmark), typeof(TypedWindowBenchmark)
        ];
        CollectionAssert.AreEquivalent(actual, CampaignCases.Types.ToArray());
        var cases = CampaignCases.Create(new CampaignOptions(), Path.GetTempPath());
        Assert.AreEqual(cases.Length, cases.Select(item => item.Definition.Id).Distinct().Count());
        Assert.AreEqual(8, cases.Count(item => item.Definition.Phase == "compilation-cold"));
        Assert.AreEqual(9, cases.Count(item => item.Definition.ResultApi == "Typed"));
        Assert.IsTrue(cases.All(item => Expectations.Ids.Contains(item.Definition.ScenarioId)));
        var cold = cases.First(item => item.Definition.Phase == "compilation-cold");
        Assert.AreEqual(1, cold.Benchmark.Job.Run.InvocationCount);
        Assert.AreEqual(1, cold.Benchmark.Job.Run.IterationCount);
        Assert.AreEqual(0, cold.Benchmark.Job.Run.WarmupCount);
        Assert.AreEqual(3, cold.Benchmark.Job.Run.LaunchCount);
    }

    [TestMethod]
    public void OfficialProfiles_PreserveFreshCompilationAndThroughputBoundaries()
    {
        var cases = CampaignCases.Create(new CampaignOptions(Mode: "Official"), Path.GetTempPath());
        var cold = cases.First(item => item.Definition.Phase == "compilation-cold").Benchmark;
        var warm = cases.First(item => item.Definition.Phase == "compilation-warm").Benchmark;
        Assert.AreEqual(8, cold.Job.Run.LaunchCount);
        Assert.AreEqual(0, cold.Job.Run.WarmupCount);
        Assert.AreEqual(1, cold.Job.Run.InvocationCount);
        Assert.AreEqual(1, cold.Job.Run.IterationCount);
        Assert.IsFalse(cold.Config.GetDiagnosers().Any(item => item is BenchmarkDotNet.Diagnosers.MemoryDiagnoser));
        Assert.AreEqual(2, warm.Job.Run.LaunchCount);
        Assert.IsTrue(warm.Config.GetDiagnosers().Any(item => item is BenchmarkDotNet.Diagnosers.MemoryDiagnoser));
    }

    [TestMethod]
    public void LinuxHardware_ParsesModelTopologyAndRam()
    {
        const string cpu = "processor : 0\nmodel name : Test CPU\nphysical id : 0\ncore id : 0\n\nprocessor : 1\nmodel name : Test CPU\nphysical id : 0\ncore id : 1\n\n";
        var result = ProvenanceCollector.ParseLinux("fixture", cpu, "MemTotal: 1024 kB\nMemFree: 512 kB\n");
        Assert.AreEqual("Test CPU", result.CpuModel);
        Assert.AreEqual(2, result.PhysicalCores);
        Assert.AreEqual(2, result.LogicalCores);
        Assert.AreEqual(1_048_576L, result.MemoryBytes);
        var withoutTopology = ProvenanceCollector.ParseLinux("fixture", "processor : 0\nHardware : ARM CPU\n", "MemTotal: 1024 kB\n");
        Assert.IsNull(withoutTopology.PhysicalCores);
        Assert.IsNotEmpty(withoutTopology.Unavailable);
    }

    [TestMethod]
    public void Store_RecoversCheckpointAndRetainsFailedAttempts()
    {
        using var temporary = new TemporaryDirectory();
        var manifest = Manifest();
        var store = new CampaignStore(temporary.Path, manifest);
        store.Save(new Measurement("case", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "failed", "synthetic failure"));
        store.Finish();
        Assert.AreEqual("incomplete", store.Manifest.Status);
        File.WriteAllText(System.IO.Path.Combine(temporary.Path, "measurements.json"), "interrupted aggregate");
        var resumed = CampaignStore.Resume(temporary.Path, Manifest());
        Assert.HasCount(1, resumed.Attempts);
        resumed.Save(Success());
        resumed.Finish();
        Assert.AreEqual("complete", resumed.Manifest.Status);
        Assert.HasCount(2, resumed.Attempts);
        Assert.Throws<InvalidOperationException>(() => CampaignStore.Resume(temporary.Path, Manifest()));
        Assert.Throws<InvalidOperationException>(() => resumed.Save(Success()));
        var measurements = ArtifactJson.Read<Measurement[]>(System.IO.Path.Combine(temporary.Path, "measurements.json"));
        Assert.HasCount(1, measurements);
        Assert.AreEqual(10d, measurements[0].MeanNanoseconds);
    }

    [TestMethod]
    public void Resume_RejectsChangedSourceVersionHardwareSuiteAndCaseDefinitions()
    {
        var original = Manifest();
        CampaignStore.ValidateResume(original, Manifest());
        var changed = Manifest("changed");
        Assert.Throws<InvalidOperationException>(() => CampaignStore.ValidateResume(original, changed));
        var provenance = original.Provenance with { Source = original.Provenance.Source with { Commit = "other" } };
        Assert.Throws<InvalidOperationException>(() => CampaignStore.ValidateResume(original, Manifest(provenance: provenance)));
        provenance = original.Provenance with { Hardware = original.Provenance.Hardware with { CpuModel = "other" } };
        Assert.Throws<InvalidOperationException>(() => CampaignStore.ValidateResume(original, Manifest(provenance: provenance)));
        provenance = original.Provenance with { Build = original.Provenance.Build with { EngineVersion = "different" } };
        Assert.Throws<InvalidOperationException>(() => CampaignStore.ValidateResume(original, Manifest(provenance: provenance)));
        var differentCases = Manifest();
        differentCases.Cases[0] = differentCases.Cases[0] with { InputCount = 99 };
        Assert.Throws<InvalidOperationException>(() => CampaignStore.ValidateResume(original, differentCases));
    }

    [TestMethod]
    public void Store_RejectsUnknownCasesAndInvalidTimings()
    {
        using var temporary = new TemporaryDirectory();
        var store = new CampaignStore(temporary.Path, Manifest());
        Assert.Throws<InvalidDataException>(() => store.Save(Success() with { CaseId = "missing" }));
        Assert.Throws<InvalidDataException>(() => store.Save(Success() with { MeanNanoseconds = double.NaN }));
        Assert.Throws<InvalidDataException>(() => store.Save(Success() with { SampleCount = 0 }));
        Assert.Throws<InvalidOperationException>(() => new CampaignStore(temporary.Path, Manifest()));
    }

    [TestMethod]
    public void RowAssertions_RejectMissingWrongAndDuplicatedValues()
    {
        Assert.Throws<AssertFailedException>(() => ResultAssertions.Rows([[1]], [], true, "missing"));
        Assert.Throws<AssertFailedException>(() => ResultAssertions.Rows([[1]], [["1"]], true, "type"));
        Assert.Throws<AssertFailedException>(() => ResultAssertions.Rows([[1], [1]], [[1], [2]], false, "duplicate"));
        ResultAssertions.Rows([[1], [2], [1]], [[2], [1], [1]], false, "bag");
    }

    [TestMethod]
    public void GitStamp_IdentifiesDetachedCheckoutWithoutGuessingBranch()
    {
        using var temporary = new TemporaryDirectory();
        ProcessCommand.Require(temporary.Path, "git", "init", "--quiet");
        File.WriteAllText(System.IO.Path.Combine(temporary.Path, "fixture.txt"), "fixture");
        ProcessCommand.Require(temporary.Path, "git", "add", "fixture.txt");
        ProcessCommand.Require(temporary.Path, "git", "-c", "user.name=Benchmark Test", "-c", "user.email=benchmark@example.invalid", "commit", "--quiet", "-m", "fixture");
        ProcessCommand.Require(temporary.Path, "git", "checkout", "--quiet", "--detach");
        var stamp = ProvenanceCollector.Collect(temporary.Path, new CampaignOptions(MachineProfile: "fixture"));
        Assert.IsNull(stamp.Source.Branch);
        Assert.IsTrue(stamp.Source.Detached);
        Assert.IsFalse(stamp.Source.Dirty);
        Assert.AreEqual(40, stamp.Source.Commit.Length);
        File.WriteAllText(System.IO.Path.Combine(temporary.Path, "fixture.txt"), "dirty");
        Assert.IsTrue(ProvenanceCollector.Collect(temporary.Path, new CampaignOptions()).Source.Dirty);
        Assert.Throws<InvalidOperationException>(() => CampaignIntegrity.ValidateSource(temporary.Path, Manifest(provenance: stamp)));
    }

    internal static Measurement Success() => new("case", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "complete", null,
        MeanNanoseconds: 10, MedianNanoseconds: 10, SampleCount: 2);

    internal static RunManifest Manifest(string suite = "suite", RunProvenance? provenance = null) => new()
    {
        SuiteVersion = suite, RunId = "fixture",
        Provenance = provenance ?? new(new("commit", "branch", [], false, null), new("profile", "cpu", 2, 4, 4, 1024, "x64", []),
            new("os", "runtime", "sdk", "bdn", true, true, 4, "Local", "Dry", false), new("version", "commit")),
        Cases = [new("case", "B01_Scan", "Expressions", "execution", "Table", "Small", 4, "shared", "core/5", "consume")]
    };
}

public sealed class TemporaryDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "musoq-benchmark-test-" + Guid.NewGuid().ToString("N"));
    public TemporaryDirectory() => Directory.CreateDirectory(Path);
    public void Dispose()
    {
        // This directory is created and owned exclusively by this test instance.
        foreach (var file in Directory.GetFiles(Path, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(Path, recursive: true);
    }
}
