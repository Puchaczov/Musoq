using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks.Scenarios;
using Musoq.Converter;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class FixtureProfileTests
{
    [TestMethod]
    [DataRow("B14_Group_ManyGroups", "B14_Group", "many-groups")]
    [DataRow("B07_EqualityJoin_Shuffled", "B07_EqualityJoin", "shuffled")]
    [DataRow("B07_DuplicateJoin_Fanout8", "B07_DuplicateJoin", "fanout8")]
    public void Variant_ReusesItsOriginalSqlAndSelectsItsProfile(string id, string original, string profile)
    {
        var scenario = ScenarioCatalog.Get(id);
        Assert.AreEqual(ScenarioCatalog.Get(original).Sql, scenario.Sql);
        Assert.AreEqual(profile, scenario.DataProfileId);
    }

    [TestMethod]
    public void ManyGroups_EightRowsProduceEightIndependentGroups()
    {
        var scenario = ScenarioCatalog.Get("B14_Group_ManyGroups");
        var expected = new ExpectedResult([("Id", typeof(int)), ("Total", typeof(decimal?)), ("Present", typeof(long))],
        [
            [0, 1m, 0L], [1, 2m, 0L], [2, 3m, 1L], [3, 4m, 0L],
            [4, 5m, 1L], [5, 6m, 0L], [6, 7m, 0L], [7, 8m, 0L]
        ]);
        using var query = InstanceCreator.CompileForExecution(scenario.Sql, "many-groups-eight",
            FixtureCatalog.Prepare(scenario, 8).CreateSchemaProvider(), BenchmarkLogger.Instance, ExecutionSettings.Create(scenario, 8));
        ResultAssertions.Execution(expected, query, scenario);
        ResultAssertions.Execution(expected, query, scenario);
    }

    [TestMethod]
    public void ShuffledRows_FollowTheFixedEightRowPermutation()
    {
        var first = Rows("B07_EqualityJoin_Shuffled", 8);
        var second = Rows("B07_EqualityJoin_Shuffled", 8);
        int[] expected = [4, 2, 3, 6, 0, 7, 1, 5];
        CollectionAssert.AreEqual(expected, first.Select(row => row.Id).ToArray());
        CollectionAssert.AreEqual(expected, second.Select(row => row.Id).ToArray());
    }

    [TestMethod]
    public void ShuffledRows_PreserveEveryField()
    {
        var actual = Rows("B07_EqualityJoin_Shuffled", 8).OrderBy(row => row.Id).Select(FixturePreparationTests.Snapshot).ToArray();
        var expected = Rows("B07_EqualityJoin", 8).Select(FixturePreparationTests.Snapshot).ToArray();
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void FanoutEight_PreservesThreeKeysAndEightOccurrencesPerKey()
    {
        var provider = FixtureCatalog.Prepare(ScenarioCatalog.Get("B07_DuplicateJoin_Fanout8"), 4).CreateSchemaProvider();
        var actual = FixturePreparationTests.ReadRows(provider, "twice").GroupBy(row => row.Key)
            .Select(group => (group.Key, group.Count())).OrderBy(item => item.Key).ToArray();
        (int, int)[] expected = [(0, 8), (1, 8), (2, 8)];
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    [DataRow("B14_Group_ManyGroups", 0)]
    [DataRow("B07_EqualityJoin_Shuffled", 0)]
    [DataRow("B07_DuplicateJoin_Fanout8", 0)]
    [DataRow("B14_Group_ManyGroups", 1_007)]
    [DataRow("B07_EqualityJoin_Shuffled", 1_007)]
    [DataRow("B07_DuplicateJoin_Fanout8", 1_007)]
    public void Profiles_PreserveTheRequestedMainInputCount(string id, int count) => Assert.HasCount(count, Rows(id, count));

    [TestMethod]
    [DataRow("B14_Group_ManyGroups")]
    [DataRow("B07_EqualityJoin_Shuffled")]
    [DataRow("B07_DuplicateJoin_Fanout8")]
    public void EmptyProfiles_PreserveTheirOutputSchema(string id)
    {
        var scenario = ScenarioCatalog.Get(id);
        var expected = new ExpectedResult(Expectations.Get(id).Columns, []);
        using var query = InstanceCreator.CompileForExecution(scenario.Sql, "empty-profile",
            FixtureCatalog.Prepare(scenario, 0).CreateSchemaProvider(), BenchmarkLogger.Instance, ExecutionSettings.Create(scenario, 0));
        ResultAssertions.Execution(expected, query, scenario);
    }

    [TestMethod]
    public void VariantCases_AddExactlyNineExecutionCases()
    {
        var variants = ScenarioCatalog.All.Where(scenario => scenario.DataProfileId != "default").Select(scenario => scenario.Id).ToHashSet(StringComparer.Ordinal);
        var cases = CampaignCases.Create(new CampaignOptions(), Path.GetTempPath()).Where(item => variants.Contains(item.Definition.ScenarioId)).ToArray();
        Assert.HasCount(9, cases);
        Assert.IsTrue(cases.All(item => item.Definition.Phase == "execution" && item.Definition.ResultApi == "Table"));
        CollectionAssert.AreEqual(new[] { 1_000, 10_000, 100_000 }, cases.Select(item => item.Definition.InputCount).Distinct().Order().ToArray());
    }

    [TestMethod]
    public void VariantCases_ExportSelectedDistributionAndProfile()
    {
        var definition = CampaignCases.Create(new CampaignOptions(Size: 7, Filter: "*Fanout8*"), Path.GetTempPath()).Single().Definition;
        Assert.AreEqual("standard", definition.Arguments!["fixtureId"]);
        Assert.AreEqual("fanout8", definition.Arguments["dataProfileId"]);
        StringAssert.Contains(definition.Distribution, "24 dimension rows");
    }

    private static FixtureRow[] Rows(string id, int count) => FixturePreparationTests.ReadRows(
        FixtureCatalog.Prepare(ScenarioCatalog.Get(id), count).CreateSchemaProvider());
}
