using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class SingleUseMaterializationPlannerTests : BasicEntityTestBase
{
    [TestMethod]
    public void PlanningText_WhenCteProjectionChainHasFilters_ShouldMarkEverySingleConsumerStage()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            """
            with base as (
                select e.Name, e.City from #A.Entities() e
            ),
            names as (
                select b.Name from base b where b.City = 'Warsaw'
            )
            select n.Name from names n
            """);
        var planningText = buildItems.RequirePlanningText();

        Assert.Contains(
            "Materialization [SingleUseProjectionFusion] cte:base -> Candidate",
            planningText);
        Assert.Contains("another single-use projection/filter stage", planningText);
        Assert.Contains(
            "Materialization [SingleUseProjectionFusion] cte:names -> Candidate",
            planningText);
        Assert.Contains("the final projection", planningText);
    }
}
