using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class CaseLifecycleTests
{
    private const string RetiredId = "B01_Scan-execution-Table-Large";

    [TestMethod]
    public void RetiringOneSize_PreservesOtherSizesTypedAndCompilationCases()
    {
        var complete = Complete();
        var catalog = Retire(complete);
        var selected = CampaignCases.Select(complete, new CampaignOptions(), catalog);
        Assert.AreEqual(complete.Length - 1, selected.Length);
        Assert.IsFalse(selected.Any(item => item.Definition.Id == RetiredId));
        Assert.IsTrue(selected.Any(item => item.Definition.Id == "B01_Scan-execution-Table-Small"));
        Assert.IsTrue(selected.Any(item => item.Definition.Id == "B01_Scan-execution-Typed-Large"));
        Assert.IsTrue(selected.Any(item => item.Definition.Id == "B01_Scan-compilation-cold-Table-ContractFixture"));
    }

    [TestMethod]
    public void ExplicitLocalInclusion_RetainsTheOriginalDefinition()
    {
        var complete = Complete();
        var selected = CampaignCases.Select(
            complete,
            new CampaignOptions(Filter: RetiredId) { IncludeRetired = true },
            Retire(complete)
        );
        Assert.HasCount(1, selected);
        Assert.AreEqual(complete.Single(item => item.Definition.Id == RetiredId).Definition, selected[0].Definition);
    }

    [TestMethod]
    public void OfficialOptions_RejectRetiredMeasurementsButAllowListing()
    {
        Assert.Throws<ArgumentException>(() =>
            CampaignOptions.Parse(["--mode", "Official", "--machine-profile", "fixture", "--include-retired"])
        );
        Assert.IsTrue(
            CampaignOptions
                .Parse(["--mode", "Official", "--machine-profile", "fixture", "--action", "List", "--include-retired"])
                .IncludeRetired
        );
        Assert.IsTrue(CampaignOptions.Parse(["--include-retired"]).IncludeRetired);
        Assert.Throws<ArgumentException>(() => CampaignOptions.Parse(["--action", "Latest", "--include-retired"]));
    }

    [TestMethod]
    public void OfficialSnapshot_RequiresEveryActiveCaseAndNoRetiredCase()
    {
        var complete = Complete();
        var catalog = Retire(complete);
        var active = CampaignCases
            .Select(complete, new CampaignOptions(), catalog)
            .Select(item => item.Definition.Id)
            .ToArray();
        catalog.ValidateSelection(active, official: true);
        Assert.Throws<InvalidDataException>(() => catalog.ValidateSelection(active.Skip(1), official: true));
        Assert.Throws<InvalidDataException>(() =>
            catalog.ValidateSelection(complete.Select(item => item.Definition.Id), official: true)
        );
    }

    [TestMethod]
    public void Snapshot_FreezesInputsAndRoundTripsWithoutALiveCatalog()
    {
        string[] known = ["old", "new"];
        BenchmarkCaseRetirement[] retired = [new("old", "Replaced", "new")];
        var snapshot = new CaseCatalogSnapshot(3, known, retired);
        known[0] = "changed";
        retired[0] = new("changed", "Changed");
        var copy = JsonSerializer.Deserialize<CaseCatalogSnapshot>(
            JsonSerializer.Serialize(snapshot, ArtifactJson.Options),
            ArtifactJson.Options
        )!;
        Assert.IsTrue(snapshot.SameAs(copy));
        Assert.AreEqual("Replaced", copy.Retirement("old")!.Reason);
        copy.ValidateSelection(["new"], official: true);
    }

    [TestMethod]
    [DataRow("unknown", "Reason", null)]
    [DataRow("old", "", null)]
    [DataRow("old", "Reason", "unknown")]
    [DataRow("old", "Reason", "old")]
    public void InvalidRetirement_IsRejected(string id, string reason, string? replacement) =>
        Assert.Throws<InvalidDataException>(() =>
            new CaseCatalogSnapshot(1, ["old", "new"], [new(id, reason, replacement)])
        );

    [TestMethod]
    public void DuplicateAndInvalidCatalogEntries_AreRejected()
    {
        Assert.Throws<InvalidDataException>(() => new CaseCatalogSnapshot(0, ["old"], []));
        Assert.Throws<InvalidDataException>(() => new CaseCatalogSnapshot(1, ["old", "old"], []));
        Assert.Throws<InvalidDataException>(() =>
            new CaseCatalogSnapshot(1, ["old"], [new("old", "A"), new("old", "B")])
        );
    }

    [TestMethod]
    public void ChangedCatalogRevision_RejectsResumeWithoutChangingWorkloads()
    {
        var saved = Manifest(new(1, ["case"], []));
        var changed = Manifest(new(2, ["case"], []));
        Assert.Throws<CampaignCompatibilityException>(() => CampaignStore.ValidateResume(saved, changed));
        CampaignStore.ValidateResume(saved, Manifest(new(1, ["case"], [])));
    }

    [TestMethod]
    public void CompletedSnapshot_VerifiesWithRetiredHistoryAndWithoutSourceCatalog()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "case-lifecycle.json");
        File.WriteAllText(path, "{\"revision\":1,\"retiredCases\":[{\"caseId\":\"old\",\"reason\":\"Retired\"}]}");
        var catalog = CaseLifecycleCatalog.Read(path, ["case", "old"]);
        var store = new CampaignStore(Path.Combine(temporary.Path, "campaign"), Manifest(catalog));
        store.Save(CampaignTests.Success());
        store.Finish();
        File.Delete(path);
        Assert.IsTrue(CampaignInspection.Verify(store.DirectoryPath).Verified);
        Assert.AreEqual(
            "Retired",
            CampaignStore.Definition(store.DirectoryPath).CaseCatalog!.Retirement("old")!.Reason
        );
    }

    private static CampaignCase[] Complete() =>
        CampaignCases.CreateComplete(new CampaignOptions(Mode: "Official"), Path.GetTempPath());

    private static CaseCatalogSnapshot Retire(CampaignCase[] complete) =>
        new(1, complete.Select(item => item.Definition.Id).ToArray(), [new(RetiredId, "Use smaller nightly sizes")]);

    private static RunManifest Manifest(CaseCatalogSnapshot catalog)
    {
        var original = CampaignTests.Manifest();
        return new RunManifest
        {
            SuiteVersion = original.SuiteVersion,
            RunId = original.RunId,
            Provenance = original.Provenance,
            Cases = original.Cases,
            CaseCatalog = catalog,
        };
    }
}
