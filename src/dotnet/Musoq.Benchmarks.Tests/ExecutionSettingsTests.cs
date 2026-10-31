using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks.Scenarios;
using Musoq.Evaluator;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class ExecutionSettingsTests
{
    [TestMethod]
    [DataRow("B22_Recursive", 0, 1)]
    [DataRow("B22_Recursive", 1_000, 1_000)]
    [DataRow("B22_Recursive", 10_000, 10_000)]
    [DataRow("B22_Recursive", 100_000, 100_000)]
    [DataRow("B22_Recursive", 1_007, 1_007)]
    [DataRow("X09_RecursiveChunks", 0, 2)]
    [DataRow("X09_RecursiveChunks", 1_000, 1_002)]
    [DataRow("X09_RecursiveChunks", 10_000, 10_002)]
    [DataRow("X09_RecursiveChunks", 100_000, 100_002)]
    [DataRow("X09_RecursiveChunks", 1_007, 1_009)]
    public void RecursiveSettings_AllowActualDepthAndDeclaredTerminationSteps(string id, int count, int expected)
    {
        var options = ExecutionSettings.Create(ScenarioCatalog.Get(id), count);
        Assert.AreEqual(expected, options.RecursiveCteLimits.MaxIterations);
    }

    [TestMethod]
    public void RecursiveSettings_PreserveEngineRowAndSnapshotLimits()
    {
        var defaults = new RecursiveCteExecutionLimits();
        var limits = ExecutionSettings.Create(ScenarioCatalog.Get("X09_RecursiveChunks"), 100_000).RecursiveCteLimits;
        Assert.AreEqual(defaults.MaxRows, limits.MaxRows);
        Assert.AreEqual(defaults.MaxSnapshotRows, limits.MaxSnapshotRows);
    }

    [TestMethod]
    public void OrdinarySettings_PreserveEngineIterationLimit()
    {
        var defaults = new RecursiveCteExecutionLimits();
        var limits = ExecutionSettings.Create(ScenarioCatalog.Get("B01_Scan"), 100_000).RecursiveCteLimits;
        Assert.AreEqual(defaults.MaxIterations, limits.MaxIterations);
    }

    [TestMethod]
    [DataRow(0, 100_000)]
    [DataRow(1_000, 100_000)]
    [DataRow(10_000, 100_000)]
    [DataRow(100_000, 300_001)]
    [DataRow(100_007, 300_022)]
    public void StructuredLimits_AllowOneCollectionAndThreeNodesPerRecord(int count, int expected)
    {
        Assert.AreEqual(expected, FixtureSchema.StructuredLimits(count).MaxNodes);
    }

    [TestMethod]
    public void StructuredLimits_PreserveEngineDepthAndStringBudgets()
    {
        var defaults = Musoq.Schema.StructuralInputs.StructuralInputLimits.Default;
        var limits = FixtureSchema.StructuredLimits(100_000);
        Assert.AreEqual(defaults.MaxDepth, limits.MaxDepth);
        Assert.AreEqual(defaults.MaxStringBytes, limits.MaxStringBytes);
    }

    [TestMethod]
    public void CaseArguments_RecordRegisteredStructuredInputBudget()
    {
        var cases = CampaignCases.Create(new CampaignOptions(Size: 100_000, Filter: "*B23_StructuredInput*"), Path.GetTempPath());
        Assert.AreEqual("300001", cases.Single().Definition.Arguments!["structuralMaxNodes"]);
    }

    [TestMethod]
    public void CaseArguments_RecordActualExecutionAndFixedCompilationDepths()
    {
        var cases = CampaignCases.Create(new CampaignOptions(Size: 10_000, Filter: "*B22_Recursive*"), Path.GetTempPath());
        var actual = cases.Select(item => $"{item.Definition.Phase}:{item.Definition.Arguments!["recursiveMaxIterations"]}").ToArray();
        string[] expected = ["compilation-cold:4", "compilation-warm:4", "execution:10000"];
        CollectionAssert.AreEquivalent(expected, actual);
    }
}
