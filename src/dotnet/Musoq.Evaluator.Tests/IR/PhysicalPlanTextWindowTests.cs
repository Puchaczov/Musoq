using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.Tests.Schema.Basic;
using PhysicalPlanPrinter = Musoq.Evaluator.IR.Physical.PhysicalPlanPrinter;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class PhysicalPlanTextWindowTests : BasicEntityTestBase
{
    [TestMethod]
    public void Print_WhenWindowOrderQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.Name, RowNumber() over (order by e.Name) as RowNum from #A.Entities() e");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [e.Name as e.Name, WindowRef(0) as RowNum]",
                "    PhysicalWindow [RowNumber(idx:0; order: e.Name)]",
                "      PhysicalMaterialize",
                "        PhysicalSchemaScan [#A.Entities() as e]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenWindowPartitionFrameQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.Name, Sum(e.Population) over (partition by e.City order by e.Name rows between 1 preceding and current row) as RunPop from #A.Entities() e");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [e.Name as e.Name, WindowRef(0) as RunPop]",
                "    PhysicalWindow [Sum(idx:0; partition: e.City; order: e.Name; args: e.Population; frame: rows between 1 preceding and current row)]",
                "      PhysicalMaterialize",
                "        PhysicalSchemaScan [#A.Entities() as e]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenQualifyQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.Name, RowNumber() over (order by e.Name) as RowNum from #A.Entities() e qualify RowNumber() over (order by e.Name) <= 2");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [e.Name as e.Name, WindowRef(0) as RowNum]",
                "    PhysicalQualify [(WindowRef(0) <= 2)]",
                "      PhysicalWindow [RowNumber(idx:0; order: e.Name)]",
                "        PhysicalMaterialize",
                "          PhysicalSchemaScan [#A.Entities() as e]"),
            planText);
    }
}
