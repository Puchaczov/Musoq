using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class CampaignRecoveryTests
{
    [TestMethod]
    public void OfficialBuild_RejectsLauncherBinariesFromAnotherCommit()
    {
        var provenance = CampaignTests.Manifest().Provenance;
        Assert.Throws<ArgumentException>(() => CampaignIntegrity.ValidateOfficialBuild(provenance));
        CampaignIntegrity.ValidateOfficialBuild(provenance with
        {
            Build = provenance.Build with { EngineInformationalVersion = "version+commit", SuiteInformationalVersion = "suite+commit" }
        });
    }

    [TestMethod]
    public void GitHistoryAttributes_PreserveRawEvidenceBytesWithoutLineEndingConversion()
    {
        using var temporary = new TemporaryDirectory();
        ProcessCommand.Require(temporary.Path, "git", "init", "--quiet");
        ProcessCommand.Require(temporary.Path, "git", "config", "core.autocrlf", "true");
        File.Copy(Path.Combine(ProvenanceCollector.RepositoryRoot(), ".gitattributes"), Path.Combine(temporary.Path, ".gitattributes"));
        const string relative = "benchmarks/history/fixture/raw/report.json";
        var evidence = Path.Combine(temporary.Path, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(evidence)!);
        File.WriteAllText(evidence, "{\r\n  \"Mean\": 10\r\n}\r\n");
        ProcessCommand.Require(temporary.Path, "git", "add", relative);
        var indexHash = ProcessCommand.Require(temporary.Path, "git", "rev-parse", ":" + relative);
        Assert.AreEqual(ProcessCommand.Require(temporary.Path, "git", "hash-object", "--no-filters", relative), indexHash);
    }

    [TestMethod]
    public void FailedPreflight_PreservesFailureEvidenceAndCannotCompleteCampaign()
    {
        using var temporary = new TemporaryDirectory();
        var store = new CampaignStore(temporary.Path, CampaignTests.Manifest());
        Assert.Throws<InvalidOperationException>(() => CampaignRunner.RecordTests(store, new(1, "assertion failed", "test error")));
        store.Finish();
        Assert.AreEqual("failed", store.Manifest.Preflight);
        Assert.AreEqual("incomplete", store.Manifest.Status);
        Assert.HasCount(0, store.Attempts);
        var evidence = Directory.GetFiles(Path.Combine(temporary.Path, "raw", "preflight"), "*.json").Single();
        Assert.AreEqual(1, ArtifactJson.Read<CommandResult>(evidence).ExitCode);
    }

    [TestMethod]
    public void QueryProvenance_RejectsDifferentSqlOrActualInputCount()
    {
        var definition = CampaignTests.Manifest().Cases[0];
        var stamp = MeasuredProcess.Capture() with
        {
            ScenarioId = definition.ScenarioId, Sql = definition.Sql, InputCount = definition.InputCount
        };
        CampaignIntegrity.ValidateQuery(stamp, definition);
        Assert.Throws<InvalidDataException>(() => CampaignIntegrity.ValidateQuery(stamp with { Sql = "different" }, definition));
        Assert.Throws<InvalidDataException>(() => CampaignIntegrity.ValidateQuery(stamp with { InputCount = 99 }, definition));
    }

    [TestMethod]
    public void UnfinishedAttempt_RerunsWithDistinctIdentityAndRetainsOrphanedOutput()
    {
        using var temporary = new TemporaryDirectory();
        var store = new CampaignStore(temporary.Path, CampaignTests.Manifest());
        var first = store.Begin("case");
        Directory.CreateDirectory(store.RawDirectory(first));
        File.WriteAllText(Path.Combine(store.RawDirectory(first), "orphan.json"), "unfinished");
        var resumed = CampaignStore.Resume(temporary.Path, CampaignTests.Manifest());
        var second = resumed.Begin("case");
        Assert.AreNotEqual(first.Id, second.Id);
        Assert.HasCount(1, resumed.RecoveryNotes);
        Assert.IsFalse(resumed.Completed("case"));
        Assert.IsTrue(File.Exists(Path.Combine(store.RawDirectory(first), "orphan.json")));
    }

    [TestMethod]
    public void FinalizedAttempt_SurvivesMissingMutableViewsAndAbandonedTemporaryFiles()
    {
        using var temporary = new TemporaryDirectory();
        var store = new CampaignStore(temporary.Path, CampaignTests.Manifest());
        store.Save(CampaignTests.Success());
        File.Delete(Path.Combine(temporary.Path, "manifest.json"));
        File.WriteAllText(Path.Combine(temporary.Path, "measurements.json"), "partial");
        File.WriteAllText(Path.Combine(temporary.Path, "abandoned.tmp"), "partial");
        var resumed = CampaignStore.Resume(temporary.Path, CampaignTests.Manifest());
        Assert.IsTrue(resumed.Completed("case"));
        resumed.Finish();
        Assert.AreEqual("complete", resumed.Manifest.Status);
    }

    [TestMethod]
    public void DamagedRawEvidence_InvalidatesOnlyItsFinalizedAttempt()
    {
        using var temporary = new TemporaryDirectory();
        var store = new CampaignStore(temporary.Path, CampaignTests.Manifest());
        var attempt = store.Begin("case");
        var raw = Path.Combine(store.RawDirectory(attempt), "report.json");
        Directory.CreateDirectory(Path.GetDirectoryName(raw)!);
        File.WriteAllText(raw, "{}");
        store.Complete(attempt, CampaignTests.Success() with { RawReports = [Path.GetRelativePath(temporary.Path, raw)] });
        File.WriteAllText(raw, "damaged");
        var resumed = CampaignStore.Resume(temporary.Path, CampaignTests.Manifest());
        Assert.IsFalse(resumed.Completed("case"));
        StringAssert.Contains(resumed.RecoveryNotes.Single(), "Damaged JSON artifact");
    }

    [TestMethod]
    public void Resume_RestoresSavedCustomInvocationAndRejectsConflictingOverride()
    {
        using var temporary = new TemporaryDirectory();
        var original = CampaignTests.Manifest();
        var saved = new RunManifest
        {
            SuiteVersion = original.SuiteVersion,
            RunId = original.RunId, Provenance = original.Provenance, Cases = original.Cases,
            Invocation = new(Job: "Dry", Filter: "*Join*", Size: 17, MachineProfile: "fixture")
        };
        _ = new CampaignStore(temporary.Path, saved);
        var options = CampaignOptions.Parse(["--resume", temporary.Path]);
        Assert.AreEqual("Dry", options.Job);
        Assert.AreEqual(17, options.Size);
        Assert.AreEqual("*Join*", options.Filter);
        Assert.Throws<CampaignCompatibilityException>(() => CampaignOptions.Parse(["--resume", temporary.Path, "--size", "18"]));
    }

    [TestMethod]
    public void Resume_UsesExactSqlArgumentsAndRuntimeSettings()
    {
        var saved = CampaignTests.Manifest();
        var changed = CampaignTests.Manifest();
        changed.Cases[0] = changed.Cases[0] with { Sql = "different" };
        Assert.Throws<CampaignCompatibilityException>(() => CampaignStore.ValidateResume(saved, changed));
        changed.Cases[0] = saved.Cases[0] with { Arguments = new() { ["workerLimit"] = "1" } };
        Assert.Throws<CampaignCompatibilityException>(() => CampaignStore.ValidateResume(saved, changed));
        var provenance = saved.Provenance with { Environment = saved.Provenance.Environment with { Sdk = "other" } };
        Assert.Throws<CampaignCompatibilityException>(() => CampaignStore.ValidateResume(saved, CampaignTests.Manifest(provenance: provenance)));
        provenance = saved.Provenance with { Environment = saved.Provenance.Environment with { MaxDegreeOfParallelism = 1 } };
        Assert.Throws<CampaignCompatibilityException>(() => CampaignStore.ValidateResume(saved, CampaignTests.Manifest(provenance: provenance)));
    }

    [TestMethod]
    public void CompletedBundle_RejectsFlushAndHasCompletionMarker()
    {
        using var temporary = new TemporaryDirectory();
        var store = new CampaignStore(temporary.Path, CampaignTests.Manifest());
        store.Save(CampaignTests.Success());
        store.Finish();
        var marker = ArtifactJson.Read<CampaignCompletion>(Path.Combine(temporary.Path, "complete.json"));
        Assert.AreEqual("3", marker.SchemaVersion);
        Assert.AreEqual(1, marker.Expected);
        Assert.AreEqual(1, marker.Completed);
        Assert.IsTrue(CampaignInspection.Verify(temporary.Path).Verified);
        Assert.Throws<InvalidOperationException>(store.Flush);
    }

    [TestMethod]
    public void ProcessProvenance_RejectsChangedRevisionAndExecutionEnvironment()
    {
        var stamp = MeasuredProcess.Capture();
        var expected = CampaignTests.Manifest().Provenance with
        {
            Hardware = CampaignTests.Manifest().Provenance.Hardware with { EffectiveProcessors = stamp.EffectiveProcessors },
            Environment = CampaignTests.Manifest().Provenance.Environment with { Runtime = stamp.Runtime, ServerGc = stamp.ServerGc },
            Build = CampaignTests.Manifest().Provenance.Build with {
                EngineInformationalVersion = stamp.EngineInformationalVersion, SuiteInformationalVersion = stamp.SuiteInformationalVersion }
        };
        CampaignIntegrity.ValidateProcess(stamp, expected);
        Assert.Throws<InvalidDataException>(() => CampaignIntegrity.ValidateProcess(stamp with { EffectiveProcessors = 0 }, expected));
        Assert.Throws<InvalidDataException>(() => CampaignIntegrity.ValidateProcess(stamp with { EngineInformationalVersion = "different" }, expected));
    }
}
