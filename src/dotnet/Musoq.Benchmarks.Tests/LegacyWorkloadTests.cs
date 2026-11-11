using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class LegacyWorkloadTests
{
    [TestMethod]
    public void OriginalOfficialCases_PreserveTheirSavedWorkloadDefinitions() =>
        AssertPreserved("20261028T235933572Z-507c7340ade4", 216);

    [TestMethod]
    public void AllExistingOfficialCases_PreserveTheirSavedWorkloadDefinitions() =>
        AssertPreserved("20261101T001222673Z-6e583af1577e", 225);

    private static void AssertPreserved(string runId, int count)
    {
        var archive = Path.Combine(
            ProvenanceCollector.RepositoryRoot(),
            "benchmarks",
            "history",
            "engine-v3",
            runId,
            "definition.json"
        );
        var expected = ArtifactJson.Read<RunManifest>(archive).Cases;
        var current = CampaignCases
            .CreateComplete(new CampaignOptions(Mode: "Official"), Path.GetTempPath())
            .ToDictionary(item => item.Definition.Id, item => item.Definition, StringComparer.Ordinal);
        Assert.HasCount(count, expected);
        CollectionAssert.AreEqual(
            expected.Select(PortableDefinition).ToArray(),
            expected.Select(item => PortableDefinition(current[item.Id])).ToArray()
        );
    }

    private static string PortableDefinition(CaseDefinition definition)
    {
        var arguments = new SortedDictionary<string, string>(definition.Arguments!, StringComparer.Ordinal);
        arguments.Remove("maxDegreeOfParallelism");
        // C# raw SQL strings inherit Git checkout line endings; the original scripts contain no multiline literal values.
        return JsonSerializer.Serialize(
            definition with
            {
                Arguments = arguments,
                Sql = definition.Sql.ReplaceLineEndings("\n"),
            },
            ArtifactJson.Options
        );
    }
}
