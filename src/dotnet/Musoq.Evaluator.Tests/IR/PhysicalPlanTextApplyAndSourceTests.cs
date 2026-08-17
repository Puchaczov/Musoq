using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhysicalPlanPrinter = Musoq.Evaluator.IR.Physical.PhysicalPlanPrinter;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class PhysicalPlanTextApplyAndSourceTests
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

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [a.City as a.City, a.Country as a.Country, b.Money as b.Money]",
                "    PhysicalNestedLoopApply [Cross]",
                "      PhysicalSchemaScan [#schema.first() as a]",
                "      PhysicalSchemaScan [#schema.second(a.Country) as b]",
                "  PhysicalProject [a.City as a.City, b.Money as b.Money]",
                "    PhysicalCteRef [ab as ab]"),
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

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [b.Value as b.Value]",
                "    PhysicalNestedLoopApply [Cross]",
                "      PhysicalSchemaScan [#schema.first() as a]",
                "      PhysicalAccessMethodSource [JustReturnArrayOfString() as b] [apply: Cross] [type: String[]]",
                "  PhysicalProject [b.Value as b.Value]",
                "    PhysicalCteRef [ab as ab]"),
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

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [p.Name as p.Name, p.Tags as p.Tags, t.Value as t.Value]",
                "    PhysicalNestedLoopApply [Cross]",
                "      PhysicalSchemaScan [#schema.first() as p]",
                "      PhysicalPropertySource [p.Tags as t] [apply: Cross] [type: String[]]",
                "  PhysicalProject [p.Name as p.Name, t.Value as t.Value]",
                "    PhysicalCteRef [pt as pt]"),
            planText);
    }
}
