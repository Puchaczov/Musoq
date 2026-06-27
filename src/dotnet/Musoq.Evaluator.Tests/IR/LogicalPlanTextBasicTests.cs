using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class LogicalPlanTextBasicTests : BasicEntityTestBase
{
    [TestMethod]
    public void Print_WhenSimpleSelectQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>("select e.Name from #A.Entities() e");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [e.Name as e.Name]",
                "    SchemaScan [#A.Entities() as e]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenSelectWithCompoundWhereQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.Name from #A.Entities() e where e.Population > 100 and e.NullableValue is not null");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [e.Name as e.Name]",
                "    Filter [((e.Population > 100) AND e.NullableValue IS NOT NULL)]",
                "      SchemaScan [#A.Entities() as e]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenOrderSkipTakeQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.Name from #A.Entities() e order by e.Name desc skip 5 take 10");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Take [10]",
                "    Skip [5]",
                "      Sort [e.Name DESC]",
                "        Project [e.Name as e.Name]",
                "          SchemaScan [#A.Entities() as e]"),
            planText);
    }
}
