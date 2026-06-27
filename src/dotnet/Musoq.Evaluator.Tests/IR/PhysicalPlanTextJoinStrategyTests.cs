using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.Tests.Schema.Basic;
using PhysicalPlanPrinter = Musoq.Evaluator.IR.Physical.PhysicalPlanPrinter;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class PhysicalPlanTextJoinStrategyTests : BasicEntityTestBase
{
    [TestMethod]
    public void Print_WhenInnerEquiJoinQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.City from #A.Entities() a join #B.Entities() b on a.Id = b.Id");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [a.Name as a.Name, a.Id as a.Id, b.City as b.City, b.Id as b.Id]",
                "    PhysicalHashJoin [Inner] [build: b.Id] [probe: a.Id]",
                "      PhysicalSchemaScan [#A.Entities() as a]",
                "      PhysicalSchemaScan [#B.Entities() as b]",
                "  PhysicalProject [a.Name as a.Name, b.City as b.City]",
                "    PhysicalCteRef [ab as ab]"),
            planText);
    }

    [TestMethod]
    public void PlanningText_WhenHashJoinBuildHasHiddenKeyPayload_ShouldShowAppliedRowWidthPruning()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.City from #A.Entities() a join #B.Entities() b on a.Id = b.Id");

        var planningText = buildItems.RequirePlanningText();

        Assert.Contains("pruning: hash-join-build:0 HashJoinBuild -> Applied", planningText);
        Assert.Contains("pruned: b.Id", planningText);
        Assert.Contains("retained: a.Name, b.City", planningText);
        Assert.Contains("HashJoinBuild row-width pruning drops build-side payload columns", planningText);
    }

    [TestMethod]
    public void PlanningText_WhenCrossJoinUsesNestedLoop_ShouldReportCardinalityRiskDiagnostic()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.Name from #A.Entities() a cross join #B.Entities() b");

        var planningText = buildItems.RequirePlanningText();

        Assert.Contains("JoinStrategy [NestedLoopCardinalityRisk] Cross ->", planningText);
    }

    [TestMethod]
    public void Print_WhenRightOuterJoinQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.Name from #A.Entities() a right outer join #B.Entities() b on a.Id = b.Id");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [a.Name as a.Name, a.Id as a.Id, b.Name as b.Name, b.Id as b.Id]",
                "    PhysicalHashJoin [RightOuter] [build: a.Id] [probe: b.Id]",
                "      PhysicalSchemaScan [#A.Entities() as a]",
                "      PhysicalSchemaScan [#B.Entities() as b]",
                "  PhysicalProject [a.Name as a.Name, b.Name as b.Name]",
                "    PhysicalCteRef [ab as ab]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenFullOuterEquiJoinQuery_ShouldUseHashJoin()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.Name from #A.Entities() a full outer join #B.Entities() b on a.Id = b.Id");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        Assert.Contains(
            "Hash join selected because at least one equi key pair was found.",
            buildItems.RequirePlanningText());
        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [a.Name as a.Name, a.Id as a.Id, b.Name as b.Name, b.Id as b.Id]",
                "    PhysicalHashJoin [FullOuter] [build: b.Id] [probe: a.Id]",
                "      PhysicalSchemaScan [#A.Entities() as a]",
                "      PhysicalSchemaScan [#B.Entities() as b]",
                "  PhysicalProject [a.Name as a.Name, b.Name as b.Name]",
                "    PhysicalCteRef [ab as ab]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenNonEquiJoinQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.Name from #A.Entities() a join #B.Entities() b on a.Population > b.Population");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [a.Name as a.Name, a.Population as a.Population, b.Name as b.Name, b.Population as b.Population]",
                "    PhysicalSortMergeJoin [Inner] [left: a.Population] [right: b.Population] [op: >] [residual: (a.Population > b.Population)]",
                "      PhysicalSchemaScan [#A.Entities() as a]",
                "      PhysicalSchemaScan [#B.Entities() as b]",
                "  PhysicalProject [a.Name as a.Name, b.Name as b.Name]",
                "    PhysicalCteRef [ab as ab]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenAsofJoinQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.Name from #A.Entities() a asof join #B.Entities() b on a.Country = b.Country and a.Population >= b.Population");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [a.Name as a.Name, a.Country as a.Country, a.Population as a.Population, b.Name as b.Name, b.Country as b.Country, b.Population as b.Population]",
                "    PhysicalNestedLoopJoin [AsofInner] [((a.Country = b.Country) AND (a.Population >= b.Population))]",
                "      PhysicalSchemaScan [#A.Entities() as a]",
                "      PhysicalSchemaScan [#B.Entities() as b]",
                "  PhysicalProject [a.Name as a.Name, b.Name as b.Name]",
                "    PhysicalCteRef [ab as ab]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenSemiJoinQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name from #A.Entities() a semi join #B.Entities() b on a.Id = b.Id");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [a.Name as a.Name]",
                "    PhysicalHashJoin [LeftSemi] [build: b.Id] [probe: a.Id]",
                "      PhysicalSchemaScan [#A.Entities() as a]",
                "      PhysicalSchemaScan [#B.Entities() as b]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenAntiJoinQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name from #A.Entities() a anti join #B.Entities() b on a.Id = b.Id");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [a.Name as a.Name]",
                "    PhysicalHashJoin [LeftAntiSemi] [build: b.Id] [probe: a.Id]",
                "      PhysicalSchemaScan [#A.Entities() as a]",
                "      PhysicalSchemaScan [#B.Entities() as b]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenCrossJoinQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.City from #A.Entities() a cross join #B.Entities() b");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        Assert.Contains("CROSS JOIN semantics require nested-loop Cartesian evaluation.", buildItems.RequirePlanningText());
        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [a.Name as a.Name, b.City as b.City]",
                "    PhysicalNestedLoopJoin [Cross] [TRUE]",
                "      PhysicalSchemaScan [#A.Entities() as a]",
                "      PhysicalSchemaScan [#B.Entities() as b]",
                "  PhysicalProject [a.Name as a.Name, b.City as b.City]",
                "    PhysicalCteRef [ab as ab]"),
            planText);
    }
}
