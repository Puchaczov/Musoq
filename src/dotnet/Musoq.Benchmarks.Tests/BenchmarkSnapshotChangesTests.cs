using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks.Scenarios;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class BenchmarkSnapshotChangesTests
{
    private static readonly BenchmarkSnapshotCase Scan = new(
        CampaignTests.Manifest().Cases[0],
        "B01_Scan_BenchmarkTable.cs",
        null
    );

    [TestMethod]
    [DataRow("Logical Plan", "logical")]
    [DataRow("Physical Plan", "physical")]
    [DataRow("Execution IR", "execution")]
    [DataRow("Generated C#", "generated")]
    public void ChangingAnInspectionSectionSelectsTheMappedCase(string section, string text)
    {
        var manifest = new BenchmarkSnapshotManifest("suite", [Scan]);
        var changes = BenchmarkSnapshotChanges.Compare(
            manifest,
            manifest,
            _ => Sample,
            _ => Sample.Replace(text, "different", StringComparison.Ordinal)
        );

        var change = changes.Single();
        Assert.AreEqual("changed", change.Status);
        Assert.AreEqual(Scan.Definition.Id, change.Filter);
        CollectionAssert.AreEqual(new[] { section }, change.ChangedSections);
    }

    [TestMethod]
    public void MovingAnIdenticalSnapshotDoesNotSelectTheCase()
    {
        var baseline = new BenchmarkSnapshotManifest("suite", [Scan]);
        var candidate = new BenchmarkSnapshotManifest(
            "suite",
            [Scan with { File = "B01_Scan_BenchmarkTable_Small.cs" }]
        );

        var changes = BenchmarkSnapshotChanges.Compare(
            baseline,
            candidate,
            _ => Sample,
            _ => Sample.ReplaceLineEndings("\r\n")
        );

        Assert.IsEmpty(changes);
    }

    [TestMethod]
    public void ChangedSqlOrSizeMakesTheBaselineIncompatible()
    {
        var baseline = new BenchmarkSnapshotManifest("suite", [Scan]);
        var candidate = new BenchmarkSnapshotManifest(
            "suite",
            [Scan with { Definition = Scan.Definition with { InputCount = 8 } }]
        );

        var changedSize = BenchmarkSnapshotChanges.Compare(baseline, candidate, _ => Sample, _ => Sample).Single();
        var changedSql = BenchmarkSnapshotChanges
            .Compare(
                baseline,
                baseline,
                _ => Sample,
                _ => Sample.Replace("select 1", "select 2", StringComparison.Ordinal)
            )
            .Single();

        Assert.AreEqual("incompatible", changedSize.Status);
        StringAssert.Contains(changedSize.Reason!, "input size");
        Assert.AreEqual("incompatible", changedSql.Status);
        StringAssert.Contains(changedSql.Reason!, "SQL");
    }

    [TestMethod]
    public void NewAndRetiredCasesReportTheirMissingCounterparts()
    {
        var baseline = new BenchmarkSnapshotManifest("suite", [Scan]);
        var candidate = new BenchmarkSnapshotManifest(
            "suite",
            [Scan with { Definition = Scan.Definition with { Id = "new" } }]
        );

        var changes = BenchmarkSnapshotChanges.Compare(baseline, candidate, _ => Sample, _ => Sample);

        Assert.AreEqual("retired", changes[0].Status);
        Assert.IsNull(changes[0].Filter);
        Assert.AreEqual("added", changes[1].Status);
        Assert.AreEqual("new", changes[1].Filter);
    }

    [TestMethod]
    public void ManifestRejectsDuplicateCaseMappings()
    {
        var manifest = new BenchmarkSnapshotManifest("suite", [Scan, Scan]);

        var error = Assert.Throws<InvalidDataException>(manifest.Validate);

        StringAssert.Contains(error.Message, "Duplicate benchmark snapshot case");
    }

    [TestMethod]
    public void SnapshotWithMissingPlanSectionFailsClearly()
    {
        var malformed = Sample.Replace("Physical Plan", "Unknown Plan", StringComparison.Ordinal);

        var error = Assert.Throws<InvalidDataException>(() => BenchmarkSnapshotSections.Parse(malformed));

        StringAssert.Contains(error.Message, "Physical Plan");
    }

    [TestMethod]
    public void GitDiffReadsBaselineAndResolvesANewWorkingTreeFile()
    {
        using var temporary = new TemporaryDirectory();
        var directory = CreateSnapshotRepository(temporary.Path);
        var newFile = "B01_Scan_BenchmarkTable_Small.cs";
        WriteSnapshot(
            directory,
            Scan with
            {
                File = newFile,
            },
            Sample.Replace("generated", "different", StringComparison.Ordinal)
        );

        var diff = BenchmarkSnapshotChanges.Inspect(temporary.Path, "HEAD", null);

        Assert.IsTrue(diff.BaselineHasSnapshots);
        var change = diff.Cases.Single();
        Assert.AreEqual(Scan.File, change.BaselineFile);
        Assert.AreEqual(newFile, change.CandidateFile);
        CollectionAssert.AreEqual(new[] { "Generated C#" }, change.ChangedSections);
    }

    [TestMethod]
    public void PatchFromTheSameFilesIsRejectedWhenTheirContentHasChanged()
    {
        using var temporary = new TemporaryDirectory();
        var directory = CreateSnapshotRepository(temporary.Path);
        var file = Path.Combine(directory, "fixture.txt");
        var original = Enumerable.Range(0, 20).Select(index => $"Line {index}").ToArray();
        File.WriteAllLines(file, original);
        CommitFixture(temporary.Path);
        var firstEdit = original.ToArray();
        firstEdit[0] = "First change";
        File.WriteAllLines(file, firstEdit);
        var patch = Path.Combine(temporary.Path, ".git", "candidate.patch");
        ProcessCommand.Require(
            temporary.Path,
            "git",
            "diff",
            "--binary",
            "--full-index",
            "--no-renames",
            $"--output={patch}",
            "HEAD",
            "--"
        );
        var secondEdit = firstEdit.ToArray();
        secondEdit[^1] = "Other change";
        File.WriteAllLines(file, secondEdit);

        var error = Assert.Throws<InvalidDataException>(() =>
            BenchmarkSnapshotChanges.Inspect(temporary.Path, "HEAD", patch)
        );

        StringAssert.Contains(error.Message, "stale content");
    }

    private static string CreateSnapshotRepository(string root)
    {
        ProcessCommand.Require(root, "git", "init", "--quiet");
        var directory = Path.Combine(root, BenchmarkSnapshotManifest.RelativeDirectory);
        Directory.CreateDirectory(directory);
        WriteSnapshot(directory, Scan, Sample);
        CommitFixture(root);
        return directory;
    }

    private static void WriteSnapshot(string directory, BenchmarkSnapshotCase item, string text)
    {
        File.WriteAllText(Path.Combine(directory, item.File), text);
        ArtifactJson.Write(
            Path.Combine(directory, BenchmarkSnapshotManifest.FileName),
            new BenchmarkSnapshotManifest(ScenarioCatalog.SuiteVersion, [item])
        );
    }

    private static void CommitFixture(string root)
    {
        ProcessCommand.Require(root, "git", "add", ".");
        ProcessCommand.Require(
            root,
            "git",
            "-c",
            "user.name=Benchmark Test",
            "-c",
            "user.email=benchmark@example.invalid",
            "commit",
            "--quiet",
            "-m",
            "Synthetic benchmark snapshot fixture"
        );
    }

    private const string Sample = """
        // === Parsed Query ===
        /*
        select 1
        */

        // === Logical Plan ===
        /*
        logical
        */

        // === Physical Plan ===
        /*
        physical
        */

        // === Execution Plan ===
        /*
        execution
        */

        // === Generated C# ===

        generated
        """;
}
