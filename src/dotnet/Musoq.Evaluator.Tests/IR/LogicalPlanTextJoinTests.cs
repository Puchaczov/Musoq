using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class LogicalPlanTextJoinTests : BasicEntityTestBase
{
    [TestMethod]
    public void Print_WhenInnerEquiJoinQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.City from #A.Entities() a join #B.Entities() b on a.Id = b.Id");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [a.Name as a.Name, a.Id as a.Id, b.City as b.City, b.Id as b.Id]",
                "    Join [Inner] [(a.Id = b.Id)]",
                "      SchemaScan [#A.Entities() as a]",
                "      SchemaScan [#B.Entities() as b]",
                "  Project [a.Name as a.Name, b.City as b.City]",
                "    CteRef [ab as ab]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenRightOuterJoinQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.Name from #A.Entities() a right outer join #B.Entities() b on a.Id = b.Id");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [a.Name as a.Name, a.Id as a.Id, b.Name as b.Name, b.Id as b.Id]",
                "    Join [RightOuter] [(a.Id = b.Id)]",
                "      SchemaScan [#A.Entities() as a]",
                "      SchemaScan [#B.Entities() as b]",
                "  Project [a.Name as a.Name, b.Name as b.Name]",
                "    CteRef [ab as ab]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenFullOuterJoinQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = PlanOnlyBuildItems.Create(
            "select a.Name, b.Name from #A.Entities() a full outer join #B.Entities() b on a.Id = b.Id");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [a.Name as a.Name, a.Id as a.Id, b.Name as b.Name, b.Id as b.Id]",
                "    Join [FullOuter] [(a.Id = b.Id)]",
                "      SchemaScan [#A.Entities() as a]",
                "      SchemaScan [#B.Entities() as b]",
                "  Project [a.Name as a.Name, b.Name as b.Name]",
                "    CteRef [ab as ab]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenNonEquiJoinQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.Name from #A.Entities() a join #B.Entities() b on a.Population > b.Population");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [a.Name as a.Name, a.Population as a.Population, b.Name as b.Name, b.Population as b.Population]",
                "    Join [Inner] [(a.Population > b.Population)]",
                "      SchemaScan [#A.Entities() as a]",
                "      SchemaScan [#B.Entities() as b]",
                "  Project [a.Name as a.Name, b.Name as b.Name]",
                "    CteRef [ab as ab]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenAsofJoinQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.Name from #A.Entities() a asof join #B.Entities() b on a.Country = b.Country and a.Population >= b.Population");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [a.Name as a.Name, a.Country as a.Country, a.Population as a.Population, b.Name as b.Name, b.Country as b.Country, b.Population as b.Population]",
                "    Join [AsofInner] [((a.Country = b.Country) AND (a.Population >= b.Population))]",
                "      SchemaScan [#A.Entities() as a]",
                "      SchemaScan [#B.Entities() as b]",
                "  Project [a.Name as a.Name, b.Name as b.Name]",
                "    CteRef [ab as ab]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenSemiJoinQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name from #A.Entities() a semi join #B.Entities() b on a.Id = b.Id");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [a.Name as a.Name]",
                "    Join [LeftSemi] [(a.Id = b.Id)]",
                "      SchemaScan [#A.Entities() as a]",
                "      SchemaScan [#B.Entities() as b]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenAntiJoinQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name from #A.Entities() a anti join #B.Entities() b on a.Id = b.Id");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [a.Name as a.Name]",
                "    Join [LeftAntiSemi] [(a.Id = b.Id)]",
                "      SchemaScan [#A.Entities() as a]",
                "      SchemaScan [#B.Entities() as b]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenCrossJoinQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name, b.City from #A.Entities() a cross join #B.Entities() b");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [a.Name as a.Name, b.City as b.City]",
                "    Join [Cross] [TRUE]",
                "      SchemaScan [#A.Entities() as a]",
                "      SchemaScan [#B.Entities() as b]",
                "  Project [a.Name as a.Name, b.City as b.City]",
                "    CteRef [ab as ab]"),
            planText);
    }
}
