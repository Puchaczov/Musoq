using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class LogicalPlanTextCteAndSetTests : BasicEntityTestBase
{
    [TestMethod]
    public void Print_WhenUnionAllQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select Name from #A.Entities() union all (Name) select Name from #B.Entities()");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "SetOp [UnionAll]",
                "  MultiStatement",
                "    Project [ko3iko.Name as Name]",
                "      SchemaScan [#A.Entities() as ko3iko]",
                "  MultiStatement",
                "    Project [vo04qt.Name as Name]",
                "      SchemaScan [#B.Entities() as vo04qt]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenSimpleCteQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "with cte as (select Name, City from #A.Entities()) select Name from cte");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "Cte",
                "  Definition [cte]",
                "    MultiStatement",
                "      Project [ko3iko.Name as Name, ko3iko.City as City]",
                "        SchemaScan [#A.Entities() as ko3iko]",
                "  Query",
                "    MultiStatement",
                "      Project [cte.Name as Name]",
                "        CteRef [cte as cte]"),
            planText);
    }
}
