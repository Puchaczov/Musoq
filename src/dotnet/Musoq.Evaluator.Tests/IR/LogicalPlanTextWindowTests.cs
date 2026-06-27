using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class LogicalPlanTextWindowTests : BasicEntityTestBase
{
    [TestMethod]
    public void Print_WhenWindowOrderQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.Name, RowNumber() over (order by e.Name) as RowNum from #A.Entities() e");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [e.Name as e.Name, WindowRef(0) as RowNum]",
                "    Window [RowNumber(idx:0; order: e.Name)]",
                "      SchemaScan [#A.Entities() as e]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenWindowPartitionFrameQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.Name, Sum(e.Population) over (partition by e.City order by e.Name rows between 1 preceding and current row) as RunPop from #A.Entities() e");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [e.Name as e.Name, WindowRef(0) as RunPop]",
                "    Window [Sum(idx:0; partition: e.City; order: e.Name; args: e.Population; frame: rows between 1 preceding and current row)]",
                "      SchemaScan [#A.Entities() as e]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenQualifyQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.Name, RowNumber() over (order by e.Name) as RowNum from #A.Entities() e qualify RowNumber() over (order by e.Name) <= 2");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [e.Name as e.Name, WindowRef(0) as RowNum]",
                "    Qualify [(WindowRef(0) <= 2)]",
                "      Window [RowNumber(idx:0; order: e.Name)]",
                "        SchemaScan [#A.Entities() as e]"),
            planText);
    }
}
