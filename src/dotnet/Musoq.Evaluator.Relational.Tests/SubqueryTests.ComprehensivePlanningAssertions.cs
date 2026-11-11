using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void Comprehensive_SubqueryPlanningStrategies_ShouldExposeOptimizedShapes()
    {
        const string predicateQuery = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE EXISTS (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "            )"
        );
        const string scalarQuery = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT Sum(b.Population) FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "            ) AS TotalPopulation\n"
            + "            FROM #A.entities() a"
        );
        const string derivedQuery = (
            "\n"
            + "            SELECT a.City, d.City\n"
            + "            FROM #A.entities() a\n"
            + "            CROSS APPLY (\n"
            + "                SELECT c.City, c.Country FROM #C.entities() c\n"
            + "                WHERE c.Country = a.Country\n"
            + "            ) d"
        );

        var predicate = CompileSubqueryForInspection(predicateQuery);
        var scalar = CompileSubqueryForInspection(scalarQuery);
        var derived = CompileSubqueryForInspection(derivedQuery);

        Assert.Contains("-> PredicateSemiJoin", predicate.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", predicate.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(predicate);

        Assert.Contains("-> ScalarHashSingle", scalar.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftSingle]", scalar.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(scalar);

        Assert.Contains("-> DerivedTableJoin", derived.PlanningText);
        Assert.Contains("PhysicalHashJoin [Inner]", derived.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(derived);
    }
}
