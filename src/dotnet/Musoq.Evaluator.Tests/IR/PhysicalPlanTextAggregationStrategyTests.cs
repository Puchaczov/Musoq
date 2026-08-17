using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using PhysicalPlanPrinter = Musoq.Evaluator.IR.Physical.PhysicalPlanPrinter;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class PhysicalPlanTextAggregationStrategyTests : BasicEntityTestBase
{
    [TestMethod]
    public void Print_WhenAggregateOnlyQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select Count(e.Name), Sum(e.Population) from #A.Entities() e");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [1 as 1, AggRef(e.Sum(e.Population)) as e.Sum(e.Population), AggRef(e.Count(e.Name)) as e.Count(e.Name)]",
                "    PhysicalSingleKeyAggregate [key: 1 (Int16)] [aggs: Sum(Population), Count(Name)]",
                "      PhysicalSchemaScan [#A.Entities() as e]",
                "  PhysicalProject [e.Count(e.Name) as Count(e.Name), e.Sum(e.Population) as Sum(e.Population)]",
                "    PhysicalCteRef [eScore as eScore]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenSingleKeyGroupByQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.City, Count(e.City) from #A.Entities() e group by e.City");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [e.City as e.City, AggRef(e.Count(e.City)) as e.Count(e.City)]",
                "    PhysicalSingleKeyAggregate [key: e.City (String)] [aggs: Count(City)]",
                "      PhysicalSchemaScan [#A.Entities() as e]",
                "  PhysicalProject [e.City as e.City, e.Count(e.City) as Count(e.City)]",
                "    PhysicalCteRef [eScore as eScore]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenMultiKeyGroupByQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.Country, e.City, Count(e.Name) from #A.Entities() e group by e.Country, e.City");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [e.City as e.City, e.Country as e.Country, AggRef(e.Count(e.Name)) as e.Count(e.Name)]",
                "    PhysicalValueTupleAggregate [keys: e.Country, e.City] [aggs: Count(Name)]",
                "      PhysicalSchemaScan [#A.Entities() as e]",
                "  PhysicalProject [e.Country as e.Country, e.City as e.City, e.Count(e.Name) as Count(e.Name)]",
                "    PhysicalCteRef [eScore as eScore]"),
            planText);
    }

    [TestMethod]
    public void Print_WhenHavingAndOrderByAggregateQuery_ShouldMatchReadableSnapshot()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.City, Count(e.City) as CityCount from #A.Entities() e group by e.City having Count(e.City) >= 2 order by CityCount desc");

        var planText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        PlanTextAssertions.AreEqual(
            PlanTextAssertions.FromLines(
                "PhysicalMultiStatement",
                "  PhysicalProject [e.City as e.City, AggRef(e.Count(e.City)) as e.Count(e.City)]",
                "    PhysicalHaving [(AggRef(e.Count(e.City)) >= 2)]",
                "      PhysicalSingleKeyAggregate [key: e.City (String)] [aggs: Count(City)]",
                "        PhysicalSchemaScan [#A.Entities() as e]",
                "  PhysicalSort [e.Count(e.City) DESC]",
                "    PhysicalProject [e.City as e.City, e.Count(e.City) as CityCount]",
                "      PhysicalCteRef [eScore as eScore]"),
            planText);
    }
}
