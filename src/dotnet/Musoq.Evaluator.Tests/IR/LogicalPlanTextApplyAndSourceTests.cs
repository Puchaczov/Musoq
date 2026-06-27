using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Logical;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class LogicalPlanTextApplyAndSourceTests
{
    private readonly PlanTextBuildHarness _buildHarness = new();

    [TestMethod]
    public void Print_WhenCrossApplySchemaMethodQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = _buildHarness.BuildForThreeSources(
            "select a.City, b.Money from #schema.first() a cross apply #schema.second(a.Country) b",
            [new CrossApplyUnusedAliasTests.CrossApplyClass1 { City = "City1", Country = "Country1", Population = 100 }],
            [new CrossApplyUnusedAliasTests.CrossApplyClass2 { Country = "Country1", Money = 1000, Month = "January" }],
            Array.Empty<CrossApplyUnusedAliasTests.CrossApplyClass3>());

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [a.City as a.City, a.Country as a.Country, b.Money as b.Money]",
                "    Apply [Cross]",
                "      SchemaScan [#schema.first() as a]",
                "      SchemaScan [#schema.second(a.Country) as b]",
                "  Project [a.City as a.City, b.Money as b.Money]",
                "    CteRef [ab as ab]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenCrossApplyAccessMethodQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = _buildHarness.BuildForThreeSources(
            "select b.Value from #schema.first() a cross apply a.JustReturnArrayOfString() b",
            [new CrossApplyUnusedAliasTests.CrossApplyClass1 { City = "City1", Country = "Country1", Population = 100 }],
            Array.Empty<CrossApplyUnusedAliasTests.CrossApplyClass2>(),
            Array.Empty<CrossApplyUnusedAliasTests.CrossApplyClass3>());

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [b.Value as b.Value]",
                "    Apply [Cross]",
                "      SchemaScan [#schema.first() as a]",
                "      AccessMethodSource [JustReturnArrayOfString() as b] [apply: Cross] [type: String[]]",
                "  Project [b.Value as b.Value]",
                "    CteRef [ab as ab]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenCrossApplyPropertyQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = _buildHarness.BuildForThreeSources(
            "select p.Name, t.Value from #schema.first() p cross apply p.Tags t",
            [new ExploratoryEvaluatorTestsBase.Person { Name = "John", Age = 30, Tags = ["vip"] }],
            Array.Empty<ExploratoryEvaluatorTestsBase.Order>(),
            Array.Empty<ExploratoryEvaluatorTestsBase.TreeNode>());

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [p.Name as p.Name, p.Tags as p.Tags, t.Value as t.Value]",
                "    Apply [Cross]",
                "      SchemaScan [#schema.first() as p]",
                "      PropertySource [p.Tags as t] [apply: Cross] [type: String[]]",
                "  Project [p.Name as p.Name, t.Value as t.Value]",
                "    CteRef [pt as pt]"),
            planText);
    }
}
