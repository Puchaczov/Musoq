using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void Comprehensive_SubqueryPlanningStrategies_ShouldExposeOptimizedShapes()
    {
        const string predicateQuery = @"
            SELECT a.City FROM #A.entities() a
            WHERE EXISTS (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
            )";
        const string scalarQuery = @"
            SELECT a.City, (
                SELECT Sum(b.Population) FROM #B.entities() b
                WHERE b.Country = a.Country
            ) AS TotalPopulation
            FROM #A.entities() a";
        const string derivedQuery = @"
            SELECT a.City, d.City
            FROM #A.entities() a
            CROSS APPLY (
                SELECT c.City, c.Country FROM #C.entities() c
                WHERE c.Country = a.Country
            ) d";

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
