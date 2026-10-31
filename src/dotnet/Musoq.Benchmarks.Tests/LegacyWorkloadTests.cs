using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class LegacyWorkloadTests
{
    [TestMethod]
    public void OriginalOfficialCases_PreserveTheirSavedWorkloadDefinitions()
    {
        var archive = Path.Combine(ProvenanceCollector.RepositoryRoot(), "benchmarks", "history", "engine-v3",
            "20261028T235933572Z-507c7340ade4", "definition.json");
        var expected = ArtifactJson.Read<RunManifest>(archive).Cases;
        var current = CampaignCases.Create(new CampaignOptions(Mode: "Official"), Path.GetTempPath())
            .ToDictionary(item => item.Definition.Id, item => item.Definition, StringComparer.Ordinal);
        Assert.HasCount(216, expected);
        CollectionAssert.AreEqual(expected.Select(PortableDefinition).ToArray(),
            expected.Select(item => PortableDefinition(current[item.Id])).ToArray());
    }

    private static string PortableDefinition(CaseDefinition definition)
    {
        var arguments = new SortedDictionary<string, string>(definition.Arguments!, StringComparer.Ordinal);
        arguments.Remove("maxDegreeOfParallelism");
        // C# raw SQL strings inherit Git checkout line endings; the original scripts contain no multiline literal values.
        return JsonSerializer.Serialize(definition with
        {
            Arguments = arguments, Sql = definition.Sql.ReplaceLineEndings("\n")
        }, ArtifactJson.Options);
    }
}
