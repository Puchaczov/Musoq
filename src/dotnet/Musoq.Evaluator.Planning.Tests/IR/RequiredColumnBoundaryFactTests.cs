using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class RequiredColumnBoundaryFactTests : BasicEntityTestBase
{
    [TestMethod]
    public void PlanningText_WhenAggregateBoundaryExists_ShouldReportRequiredColumnBoundaryFacts()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.City, Count(e.Name) as NameCount from #A.Entities() e group by e.City");

        AssertHasRequiredColumnBoundaryFact(buildItems.RequirePlanningText(), "Aggregate");
    }

    [TestMethod]
    public void PlanningText_WhenWindowBoundaryExists_ShouldReportRequiredColumnBoundaryFacts()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.City, RowNumber() over (partition by e.Country order by e.Population) as RowNum from #A.Entities() e");

        AssertHasRequiredColumnBoundaryFact(buildItems.RequirePlanningText(), "Window");
    }

    [TestMethod]
    public void PlanningText_WhenSetAndCteBoundariesExist_ShouldReportRequiredColumnBoundaryFacts()
    {
        var setBuildItems = CreateBuildItems<BasicEntity>(
            "select Name from #A.Entities() union all (Name) select Name from #B.Entities()");
        var cteBuildItems = CreateBuildItems<BasicEntity>(
            "with people as (select Name, City, Country from #A.Entities()) select Name from people");

        AssertHasRequiredColumnBoundaryFact(setBuildItems.RequirePlanningText(), "SetOperation");
        AssertHasRequiredColumnBoundaryFact(cteBuildItems.RequirePlanningText(), "CteMaterialization");
    }

    [TestMethod]
    public void PlanningText_WhenHashJoinExists_ShouldReportJoinEdgeRequiredColumnBoundaryFacts()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select a.Name from #A.Entities() a inner join #B.Entities() b on a.City = b.City");
        var planningText = buildItems.RequirePlanningText();

        AssertHasRequiredColumnBoundaryFact(planningText, "HashJoinBuild");
        AssertHasRequiredColumnBoundaryFact(planningText, "JoinLeftEdge");
        AssertHasRequiredColumnBoundaryFact(planningText, "JoinRightEdge");
    }

    private static void AssertHasRequiredColumnBoundaryFact(string planningText, string kind)
    {
        Assert.Contains("RequiredColumnBoundaryFacts", planningText);
        Assert.Contains(kind, planningText);
        Assert.Contains("required:", planningText);
        Assert.Contains("retained:", planningText);
        Assert.Contains("blocked:", planningText);
        Assert.Contains("projection planning", planningText);
    }
}
