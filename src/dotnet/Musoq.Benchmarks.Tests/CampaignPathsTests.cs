using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class CampaignPathsTests
{
    private static string Repository => Path.Combine(Path.GetPathRoot(Path.GetTempPath())!, "r");

    [TestMethod]
    public void EntireCatalog_FitsCompactDefaultLayout()
    {
        var cases = CampaignCases.Create(new CampaignOptions(), Path.GetTempPath());
        var directory = Path.Combine(Repository, "benchmarks", "history", "engine-v3", "20261028T235933572Z-507c7340ade4");
        CampaignPaths.Validate(Repository, directory, cases);
        Assert.AreEqual(225, cases.Length);
    }

    [TestMethod]
    public void RelativePathBudget_AcceptsBoundaryAndRejectsOneMoreCharacter()
    {
        CampaignPaths.ValidateFile(Repository, Path.Combine(Repository, new string('a', 180)));
        Assert.Throws<ArgumentException>(() => CampaignPaths.ValidateFile(Repository, Path.Combine(Repository, new string('a', 181))));
    }

    [TestMethod]
    public void AbsolutePathBudget_IncludesAtomicTemporarySuffixOutsideRepository()
    {
        var outside = Path.Combine(Path.GetPathRoot(Repository)!, "outside");
        var suffixLength = 203 - outside.Length - 1;
        CampaignPaths.ValidateFile(Repository, Path.Combine(outside, new string('a', suffixLength)));
        Assert.Throws<ArgumentException>(() => CampaignPaths.ValidateFile(Repository, Path.Combine(outside, new string('a', suffixLength + 1))));
    }

    [TestMethod]
    public void LongOutput_RejectedBeforeCampaignDirectoryIsCreated()
    {
        var output = Path.Combine(Repository, new string('a', 210));
        Assert.IsFalse(Directory.Exists(output));
        Assert.AreEqual(5, CampaignCli.Run(["--job", "Dry", "--filter", "*B01_Scan-execution-Table*",
            "--output", output, "--size", "7", "--in-process", "--json"]));
        Assert.IsFalse(Directory.Exists(output));
    }

    [TestMethod]
    public void RawDirectory_DoesNotRepeatLongCaseName()
    {
        using var temporary = new TemporaryDirectory();
        var manifest = CampaignTests.Manifest();
        var id = new string('x', 200);
        manifest.Cases[0] = manifest.Cases[0] with { Id = id };
        var store = new CampaignStore(temporary.Path, manifest);
        var attempt = store.Begin(id);
        Assert.AreEqual(Path.Combine(temporary.Path, "raw", attempt.Id), store.RawDirectory(attempt));
        var saved = ArtifactJson.Read<AttemptStart>(Path.Combine(temporary.Path, "attempts", attempt.Id, "start.json"));
        Assert.AreEqual(id, saved.CaseId);
    }
}
