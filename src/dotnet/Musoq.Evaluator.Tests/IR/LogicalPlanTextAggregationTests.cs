using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class LogicalPlanTextAggregationTests : BasicEntityTestBase
{
    [TestMethod]
    public void Print_WhenAggregateOnlyQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select Count(e.Name), Sum(e.Population) from #A.Entities() e");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [1 as 1, AggRef(e.Sum(e.Population)) as e.Sum(e.Population), AggRef(e.Count(e.Name)) as e.Count(e.Name)]",
                "    Aggregate [keys: 1] [aggs: Sum(Population), Count(Name)]",
                "      SchemaScan [#A.Entities() as e]",
                "  Project [e.Count(e.Name) as Count(e.Name), e.Sum(e.Population) as Sum(e.Population)]",
                "    CteRef [eScore as eScore]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenSingleKeyGroupByQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.City, Count(e.City) from #A.Entities() e group by e.City");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [e.City as e.City, AggRef(e.Count(e.City)) as e.Count(e.City)]",
                "    Aggregate [keys: e.City] [aggs: Count(City)]",
                "      SchemaScan [#A.Entities() as e]",
                "  Project [e.City as e.City, e.Count(e.City) as Count(e.City)]",
                "    CteRef [eScore as eScore]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenMultiKeyGroupByQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.Country, e.City, Count(e.Name) from #A.Entities() e group by e.Country, e.City");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [e.City as e.City, e.Country as e.Country, AggRef(e.Count(e.Name)) as e.Count(e.Name)]",
                "    Aggregate [keys: e.Country, e.City] [aggs: Count(Name)]",
                "      SchemaScan [#A.Entities() as e]",
                "  Project [e.Country as e.Country, e.City as e.City, e.Count(e.Name) as Count(e.Name)]",
                "    CteRef [eScore as eScore]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenHavingAndOrderByAggregateQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.City, Count(e.City) as CityCount from #A.Entities() e group by e.City having Count(e.City) >= 2 order by CityCount desc");

        var planText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "MultiStatement",
                "  Project [e.City as e.City, AggRef(e.Count(e.City)) as e.Count(e.City)]",
                "    Having [(AggRef(e.Count(e.City)) >= 2)]",
                "      Aggregate [keys: e.City] [aggs: Count(City)]",
                "        SchemaScan [#A.Entities() as e]",
                "  Sort [e.Count(e.City) DESC]",
                "    Project [e.City as e.City, e.Count(e.City) as CityCount]",
                "      CteRef [eScore as eScore]"),
            planText);
    }
}
