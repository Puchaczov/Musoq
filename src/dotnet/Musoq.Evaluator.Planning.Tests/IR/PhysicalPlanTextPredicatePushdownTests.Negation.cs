using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalPlanTextPredicatePushdownTests
{
    [TestMethod]
    [DataRow("not (a.Population < b.Population)", 1)]
    [DataRow("not (b.Population > a.Population + 2)", 1)]
    [DataRow("not (a.Population > 0 and b.Population < 2)", 1)]
    [DataRow("not (a.City rlike 'NY.*')", 1)]
    public void WhenNegationCannotTransferExactly_ShouldNotIntroduceFalseJoinInputFilter(string predicate, int filterCount)
    {
        var result = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            $"select a.Name, b.City from #A.Entities() a join #B.Entities() b on a.City = b.City where {predicate}");

        Assert.DoesNotContain("NOT (1 = 1)", result.RequirePlanningText());
        Assert.AreEqual(filterCount, PhysicalPlanPrinter.Print(result.RequirePhysicalPlan()).Split("PhysicalFilter").Length - 1,
            "Only exact predicates may remain, including conservative retention of potentially failing regex evaluation.");
    }

    [TestMethod]
    public void WhenSourceLocalNegationIsSafe_ShouldMoveOnlyToItsOwnJoinInput()
    {
        var result = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            "select a.Name, b.City from #A.Entities() a join #B.Entities() b on a.City = b.City where not (b.Population < 2)");

        Assert.Contains("movement: Where -> PreInnerJoinRight", result.RequirePlanningText());
        Assert.DoesNotContain("movement: Where -> PreInnerJoinLeft", result.RequirePlanningText());
        Assert.Contains("PhysicalFilter [NOT (b.Population < 2)]", PhysicalPlanPrinter.Print(result.RequirePhysicalPlan()));
    }
}
