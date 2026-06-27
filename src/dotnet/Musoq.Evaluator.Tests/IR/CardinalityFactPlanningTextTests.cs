using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class CardinalityFactPlanningTextTests : BasicEntityTestBase
{
    [TestMethod]
    public void PlanningText_WhenValuesSourceIsUsed_ShouldReportExactCardinalityFact()
    {
        const string query = @"
from values {
    { Name: 'Newtonsoft.Json', Score: 10 },
    { Name: 'Legacy.Package', Score: 20 },
    { Name: 'Other.Package', Score: 30 }
} packages
select packages.Name";
        var buildItems = CreateBuildItems<BasicEntity>(query);

        var planningText = buildItems.RequirePlanningText();

        Assert.Contains("CardinalityFacts", planningText);
        Assert.Contains("fact: values:packages Values -> Exact exact=3 lower=3 upper=3 confidence=1 - VALUES source has 3 row(s).", planningText);
        Assert.Contains("CardinalityFacts [CardinalityFactPlanner] values:packages -> Exact", planningText);
    }

    [TestMethod]
    public void PlanningText_WhenTakeHasNoExactInput_ShouldReportBoundedCardinalityFact()
    {
        var buildItems = CreateBuildItems<BasicEntity>("select e.Name from #A.Entities() e take 2");

        var planningText = buildItems.RequirePlanningText();

        Assert.Contains("CardinalityFacts", planningText);
        Assert.Contains("Take -> Bounded exact=null lower=0 upper=2 confidence=1 - TAKE limits output to at most 2 row(s).", planningText);
        Assert.Contains("CardinalityFacts [CardinalityFactPlanner] take:0 -> Bounded", planningText);
    }
}
