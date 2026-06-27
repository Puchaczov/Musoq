using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.Tests.Schema.Basic;
using PhysicalPlanPrinter = Musoq.Evaluator.IR.Physical.PhysicalPlanPrinter;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class PhysicalPlanTextBasicTests : BasicEntityTestBase
{
    [TestMethod]
    public void Print_WhenSimpleSelectQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>("select e.Name from #A.Entities() e");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [e.Name as e.Name]",
                "    PhysicalSchemaScan [#A.Entities() as e]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenSelectWithCompoundWhereQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.Name from #A.Entities() e where e.Population > 100 and e.NullableValue is not null");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [e.Name as e.Name]",
                "    PhysicalFilter [((e.Population > 100) AND e.NullableValue IS NOT NULL)]",
                "      PhysicalSchemaScan [#A.Entities() as e] [pushdown: (e.Population > 100), e.NullableValue IS NOT NULL]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenOrderSkipTakeQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.Name from #A.Entities() e order by e.Name desc skip 5 take 10");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalTopOffset [skip 5, take 10] [e.Name DESC]",
                "    PhysicalProject [e.Name as e.Name]",
                "      PhysicalSchemaScan [#A.Entities() as e]"),
            planText);
    }
}
