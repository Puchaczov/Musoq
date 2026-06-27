using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.Tests.Schema.Basic;
using PhysicalPlanPrinter = Musoq.Evaluator.IR.Physical.PhysicalPlanPrinter;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class PhysicalPlanTextCteAndSetTests : BasicEntityTestBase
{
    [TestMethod]
    public void Print_WhenUnionAllQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select Name from #A.Entities() union all (Name) select Name from #B.Entities()");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalSetOp [UnionAll]",
                "  PhysicalMultiStatement",
                "    PhysicalProject [ko3iko.Name as Name]",
                "      PhysicalSchemaScan [#A.Entities() as ko3iko]",
                "  PhysicalMultiStatement",
                "    PhysicalProject [vo04qt.Name as Name]",
                "      PhysicalSchemaScan [#B.Entities() as vo04qt]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenSimpleCteQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "with cte as (select Name, City from #A.Entities()) select Name from cte");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalCte",
                "  Definition [cte]",
                "    PhysicalMultiStatement",
                "      PhysicalProject [ko3iko.Name as Name, ko3iko.City as City]",
                "        PhysicalSchemaScan [#A.Entities() as ko3iko]",
                "  Query",
                "    PhysicalMultiStatement",
                "      PhysicalProject [cte.Name as Name]",
                "        PhysicalCteRef [cte as cte]"),
            planText);
    }
}
