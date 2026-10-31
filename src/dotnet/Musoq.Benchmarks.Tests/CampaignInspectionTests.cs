using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class CampaignInspectionTests
{
    [TestMethod]
    public void OfficialAttempt_RejectsMissingNativeEvidenceBeforeItCanBecomeComplete()
    {
        using var temporary = new TemporaryDirectory();
        var store = new CampaignStore(temporary.Path, Official("fixture"));
        Assert.Throws<InvalidDataException>(() => store.Save(CampaignTests.Success()));
        Assert.IsFalse(store.Completed("case"));
    }

    [TestMethod]
    public void Latest_FallsBackToOlderValidCampaignWhenNewerEvidenceIsDamaged()
    {
        using var temporary = new TemporaryDirectory();
        Complete(Path.Combine(temporary.Path, "old"), Official("first"));
        var newer = Path.Combine(temporary.Path, "new");
        Complete(newer, Official("second"));
        File.WriteAllText(Path.Combine(newer, "raw", "fixture-report-full.json"), "damaged");
        var result = CampaignInspection.Latest(temporary.Path);
        Assert.AreEqual("first", result.Selected.Single().Source.Commit);
        Assert.HasCount(1, result.Excluded);
    }

    [TestMethod]
    public void LocalDry_IsCompleteButNeverWebsiteEligible()
    {
        using var temporary = new TemporaryDirectory();
        Complete(temporary.Path, CampaignTests.Manifest());
        var receipt = CampaignInspection.Verify(temporary.Path);
        Assert.AreEqual("complete", receipt.Status);
        Assert.IsTrue(receipt.Verified);
        Assert.IsFalse(receipt.WebsiteEligible);
        Assert.IsFalse(receipt.ResumeEligible);
    }

    [TestMethod]
    public void Latest_UsesWholeCompletedCampaignAndExcludesNewerIncompleteRun()
    {
        using var temporary = new TemporaryDirectory();
        Complete(Path.Combine(temporary.Path, "old"), Official("first"));
        Complete(Path.Combine(temporary.Path, "new"), Official("second"));
        _ = new CampaignStore(Path.Combine(temporary.Path, "unfinished"), Official("third"));
        var latest = CampaignInspection.Latest(temporary.Path);
        Assert.HasCount(1, latest.Selected);
        Assert.AreEqual("second", latest.Selected[0].Source.Commit);
        Assert.IsTrue(latest.Selected[0].WebsiteEligible);
        Assert.IsTrue(latest.Selected[0].Verified);
        Assert.HasCount(1, latest.Excluded);
    }

    [TestMethod]
    public void Latest_SelectsNewestVersionAcrossMachinesAndSuites()
    {
        using var temporary = new TemporaryDirectory();
        Complete(Path.Combine(temporary.Path, "first"), Official("first"));
        Complete(Path.Combine(temporary.Path, "other-machine"), Official("second", "other-machine"));
        Complete(Path.Combine(temporary.Path, "other-suite"), Official("third", suite: "other-suite", runtime: "other-runtime"));
        var latest = CampaignInspection.Latest(temporary.Path).Selected;
        Assert.HasCount(1, latest);
        Assert.AreEqual("third", latest[0].Source.Commit);
    }

    [TestMethod]
    public void Verify_RejectsMissingCompletedSummary()
    {
        using var temporary = new TemporaryDirectory();
        Complete(temporary.Path, CampaignTests.Manifest());
        File.Delete(Path.Combine(temporary.Path, "summary.md"));
        Assert.Throws<InvalidDataException>(() => CampaignInspection.Verify(temporary.Path));
    }

    [TestMethod]
    public void UnsupportedBundle_RejectsInspectionWithoutChangingFiles()
    {
        using var temporary = new TemporaryDirectory();
        var original = CampaignTests.Manifest();
        var unsupported = new RunManifest
        {
            SchemaVersion = "999", SuiteVersion = "unsupported", RunId = "unsupported",
            Provenance = original.Provenance, Cases = original.Cases, Status = "complete"
        };
        ArtifactJson.Write(Path.Combine(temporary.Path, "definition.json"), unsupported);
        ArtifactJson.Write(Path.Combine(temporary.Path, "measurements.json"), new[] { CampaignTests.Success() });
        var before = File.ReadAllBytes(Path.Combine(temporary.Path, "definition.json"));
        Assert.Throws<InvalidDataException>(() => CampaignInspection.Verify(temporary.Path));
        CollectionAssert.AreEqual(before, File.ReadAllBytes(Path.Combine(temporary.Path, "definition.json")));
    }

    [TestMethod]
    public void Status_IdentifiesActiveAttemptWithoutLaunchingAnything()
    {
        using var temporary = new TemporaryDirectory();
        var store = new CampaignStore(temporary.Path, CampaignTests.Manifest());
        _ = store.Begin("case");
        using var process = Process.GetCurrentProcess();
        ArtifactJson.Write(Path.Combine(temporary.Path, "sessions", "fixture.json"),
            new CampaignSession(process.Id, process.StartTime.ToUniversalTime(), Environment.MachineName,
                DateTimeOffset.UtcNow));
        var receipt = CampaignInspection.Status(temporary.Path);
        Assert.IsFalse(receipt.Verified);
        Assert.IsTrue(receipt.Active);
        Assert.AreEqual("case", receipt.ActiveCase);
        Assert.AreEqual(0, receipt.Interrupted);
        Assert.IsFalse(receipt.ResumeEligible);
    }

    [TestMethod]
    public void ArtifactPaths_AcceptTrailingDirectorySeparatorAndRejectEscape()
    {
        using var temporary = new TemporaryDirectory();
        Assert.AreEqual(Path.Combine(temporary.Path, "report.json"), BundleFiles.Resolve(temporary.Path + Path.DirectorySeparatorChar, "report.json"));
        Assert.Throws<InvalidDataException>(() => BundleFiles.Resolve(temporary.Path, "../outside.json"));
    }

    [TestMethod]
    public void Status_ReportsCheckpointsWithoutAccessingRawEvidence()
    {
        using var temporary = new TemporaryDirectory();
        Complete(temporary.Path, Official("fixture"));
        var path = Path.Combine(temporary.Path, "raw", "fixture-report-full.json");
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var status = CampaignInspection.Status(temporary.Path);
            Assert.AreEqual(1, status.Completed);
            Assert.IsFalse(status.Verified);
            Assert.IsFalse(status.WebsiteEligible);
        }
        File.Delete(path);
        Assert.AreEqual(1, CampaignInspection.Status(temporary.Path).Completed);
        Assert.Throws<InvalidDataException>(() => CampaignInspection.Verify(temporary.Path));
    }

    [TestMethod]
    public void Verify_DoesNotRewriteRecordedEvidence()
    {
        using var temporary = new TemporaryDirectory();
        Complete(temporary.Path, Official("fixture"));
        var metadata = Path.Combine(temporary.Path, "raw", "measured-process-fixture.json");
        var before = File.ReadAllBytes(metadata);
        Assert.IsTrue(CampaignInspection.Verify(temporary.Path).WebsiteEligible);
        CollectionAssert.AreEqual(before, File.ReadAllBytes(metadata));
    }

    [TestMethod]
    public void Verify_RejectsInconsistentCompletionMarker()
    {
        using var temporary = new TemporaryDirectory();
        Complete(temporary.Path, CampaignTests.Manifest());
        var path = Path.Combine(temporary.Path, "complete.json");
        var marker = ArtifactJson.Read<CampaignCompletion>(path);
        ArtifactJson.Write(path, marker with { Expected = 2 });
        Assert.Throws<InvalidDataException>(() => CampaignInspection.Verify(temporary.Path));
    }

    [TestMethod]
    public void Verify_RejectsChangedSqlAndMissingPreflightEvidence()
    {
        using var temporary = new TemporaryDirectory();
        Complete(temporary.Path, Official("fixture"));
        var path = Path.Combine(temporary.Path, "raw", "measured-process-fixture.json");
        var stamp = ArtifactJson.Read<MeasuredProcessStamp>(path);
        ArtifactJson.Write(path, stamp with { Sql = "different" });
        Assert.Throws<InvalidDataException>(() => CampaignInspection.Verify(temporary.Path));
        ArtifactJson.Write(path, stamp);
        File.Delete(Path.Combine(temporary.Path, "raw", "preflight", "tests.json"));
        Assert.Throws<InvalidDataException>(() => CampaignInspection.Verify(temporary.Path));
    }

    [TestMethod]
    public void Verify_RejectsDerivedMetricsThatDifferFromFinalizedAttempts()
    {
        using var temporary = new TemporaryDirectory();
        Complete(temporary.Path, CampaignTests.Manifest());
        var path = Path.Combine(temporary.Path, "measurements.json");
        var measurements = ArtifactJson.Read<Measurement[]>(path);
        ArtifactJson.Write(path, new[] { measurements[0] with { MeanNanoseconds = 20 } });
        Assert.Throws<InvalidDataException>(() => CampaignInspection.Verify(temporary.Path));
    }

    [TestMethod]
    public void Verify_DoesNotAuthenticatePlausibleEditsToBothMetricViews()
    {
        using var temporary = new TemporaryDirectory();
        Complete(temporary.Path, CampaignTests.Manifest());
        var path = Path.Combine(temporary.Path, "measurements.json");
        var original = ArtifactJson.Read<Measurement[]>(path)[0];
        var changed = original with { MeanNanoseconds = 20 };
        ArtifactJson.Write(path, new[] { changed });
        ArtifactJson.Write(Path.Combine(temporary.Path, "attempts", changed.AttemptId!, "result.json"), changed);
        Assert.IsTrue(CampaignInspection.Verify(temporary.Path).Verified);
    }

    [TestMethod]
    public void Latest_SeparatesVersionsAndUsesRunIdForEqualCompletionTimes()
    {
        using var temporary = new TemporaryDirectory();
        var first = Path.Combine(temporary.Path, "z-directory");
        var second = Path.Combine(temporary.Path, "a-directory");
        Complete(first, Official("a-run"));
        Complete(second, Official("b-run"));
        var completed = DateTimeOffset.UtcNow;
        SetCompletion(first, completed);
        SetCompletion(second, completed);
        var other = Official("other-version");
        other = new RunManifest
        {
            SuiteVersion = other.SuiteVersion, RunId = other.RunId, Cases = other.Cases,
            Provenance = other.Provenance with { Build = other.Provenance.Build with { EngineVersion = "next" } }
        };
        Complete(Path.Combine(temporary.Path, "other"), other);
        var selected = CampaignInspection.Latest(temporary.Path).Selected;
        Assert.HasCount(2, selected);
        Assert.AreEqual("a-run", selected.Single(item => item.EngineVersion == "version").RunId);
    }

    [TestMethod]
    public void Verify_RejectsValidJsonWithoutNativeReportStructure()
    {
        using var temporary = new TemporaryDirectory();
        Complete(temporary.Path, Official("fixture"));
        File.WriteAllText(Path.Combine(temporary.Path, "raw", "fixture-report-full.json"), "[]");
        Assert.Throws<InvalidDataException>(() => CampaignInspection.Verify(temporary.Path));
    }

    [TestMethod]
    public void Verify_RejectsMalformedAttemptHistoryEvenWithAllCasesComplete()
    {
        using var temporary = new TemporaryDirectory();
        Complete(temporary.Path, CampaignTests.Manifest());
        var directory = Path.Combine(temporary.Path, "attempts", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "start.json"), "broken");
        Assert.Throws<InvalidDataException>(() => CampaignInspection.Verify(temporary.Path));
    }

    [TestMethod]
    public void Resume_RetainsSuccessfulNativeEvidenceAndRetriesInterruptedCase()
    {
        using var temporary = new TemporaryDirectory();
        var fixture = Official("fixture");
        var manifest = new RunManifest
        {
            SuiteVersion = fixture.SuiteVersion, RunId = fixture.RunId, Provenance = fixture.Provenance,
            Cases = [fixture.Cases[0], fixture.Cases[0] with { Id = "second" }]
        };
        var store = new CampaignStore(temporary.Path, manifest);
        SaveOfficialAttempt(store);
        var interrupted = store.Begin("second");
        store.Finish();
        var result = store.Attempts.Single();
        var paths = result.RawReports!.Append($"attempts/{result.AttemptId}/result.json").ToArray();
        var before = paths.Select(path => File.ReadAllBytes(BundleFiles.Resolve(temporary.Path, path))).ToArray();
        var resumed = CampaignStore.Resume(temporary.Path, manifest);
        Assert.IsTrue(resumed.Completed("case"));
        Assert.IsFalse(resumed.Completed("second"));
        var retry = resumed.Begin("second");
        Assert.AreNotEqual(interrupted.Id, retry.Id);
        AssertEvidenceUnchanged(temporary.Path, paths, before);
    }

    private static void AssertEvidenceUnchanged(string directory, string[] paths, byte[][] before)
    {
        for (var index = 0; index < paths.Length; index++)
            CollectionAssert.AreEqual(before[index], File.ReadAllBytes(BundleFiles.Resolve(directory, paths[index])));
    }

    private static void SetCompletion(string directory, DateTimeOffset completed)
    {
        var markerPath = Path.Combine(directory, "complete.json");
        var marker = ArtifactJson.Read<CampaignCompletion>(markerPath);
        ArtifactJson.Write(markerPath, marker with { CompletedUtc = completed });
        var path = Path.Combine(directory, "manifest.json");
        var manifest = ArtifactJson.Read<RunManifest>(path);
        manifest.FinishedUtc = completed;
        ArtifactJson.Write(path, manifest);
    }

    private static RunManifest Official(string commit, string machine = "profile", string suite = "suite", string? runtime = null)
    {
        var fixture = CampaignTests.Manifest();
        var stamp = MeasuredProcess.Capture();
        return new()
        {
            SuiteVersion = suite, RunId = commit, Cases = fixture.Cases,
            Provenance = fixture.Provenance with
            {
                Source = fixture.Provenance.Source with { Commit = commit },
                Hardware = fixture.Provenance.Hardware with { Profile = machine, EffectiveProcessors = stamp.EffectiveProcessors },
                Environment = fixture.Provenance.Environment with { Mode = "Official", Job = "Official", Runtime = runtime ?? stamp.Runtime, ServerGc = stamp.ServerGc },
                Build = fixture.Provenance.Build with {
                    EngineInformationalVersion = stamp.EngineInformationalVersion, SuiteInformationalVersion = stamp.SuiteInformationalVersion }
            }
        };
    }

    private static void Complete(string directory, RunManifest manifest)
    {
        var store = new CampaignStore(directory, manifest);
        if (manifest.Provenance.Environment.Mode == "Official")
        {
            SaveOfficialAttempt(store);
            store.Finish();
            return;
        }
        store.Save(CampaignTests.Success());
        store.Finish();
    }
    private static void SaveOfficialAttempt(CampaignStore store)
    {
        var directory = store.DirectoryPath;
        var manifest = store.Manifest;
        store.Manifest.Preflight = "passed";
        var native = Path.Combine(directory, "raw", "fixture-report-full.json");
        var metadata = Path.Combine(directory, "raw", "measured-process-fixture.json");
        Directory.CreateDirectory(Path.GetDirectoryName(native)!);
        File.WriteAllText(native, """{"Benchmarks":[{"Statistics":{"N":2,"Mean":10,"Median":10}}]}""");
        var stamp = MeasuredProcess.Capture() with
        {
            Source = manifest.Provenance.Source, Runtime = manifest.Provenance.Environment.Runtime,
            ScenarioId = manifest.Cases[0].ScenarioId, Sql = manifest.Cases[0].Sql, InputCount = manifest.Cases[0].InputCount
        };
        ArtifactJson.Write(metadata, stamp);
        ArtifactJson.Write(Path.Combine(directory, "raw", "preflight", "tests.json"), new CommandResult(0, "passed", ""));
        store.Save(CampaignTests.Success() with { RawReports = ["raw/fixture-report-full.json", "raw/measured-process-fixture.json"] });
    }

}
