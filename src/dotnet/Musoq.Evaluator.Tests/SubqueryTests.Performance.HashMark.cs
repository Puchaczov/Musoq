using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenPredicateSubqueryProducesValue_ShouldUseBoundedHashMarkProbe()
    {
        const string query = @"
            SELECT a.City,
                   CASE WHEN EXISTS (
                       SELECT b.City FROM #B.entities() b
                       WHERE b.Country = a.Country
                   ) THEN 'Y' ELSE 'N' END AS HasMatch
            FROM #A.entities() a";

        var inspection = CompileSubqueryForInspection(query);

        Assert.Contains("SubqueryStrategy [SubqueryLoweringStrategy] _sq_1 -> PredicateHashMark", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftMark]", inspection.PhysicalPlanText);
        Assert.Contains("CreateKeySet [_sq_1Keys", inspection.ExecutionPlanText);
        Assert.Contains("KeySetProbe", inspection.ExecutionPlanText);
        Assert.IsFalse(inspection.ExecutionPlanText.Contains("CreateHash [", System.StringComparison.Ordinal));
        Assert.DoesNotContain("_sq_1.", inspection.ExecutionPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenPredicateHashMarkDisablesGeneralHashJoins_ShouldRetainBoundedHashStrategy()
    {
        const string query = @"
            SELECT a.City,
                   CASE WHEN EXISTS (
                       SELECT b.City FROM #B.entities() b
                       WHERE b.Country = a.Country
                   ) THEN 'Y' ELSE 'N' END AS HasMatch
            FROM #A.entities() a";
        var options = new CompilationOptions(
            useHashJoin: false,
            useSortMergeJoin: false,
            usePrimitiveTypeValidation: false);

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources(), options)
            .Run(TestContext.CancellationToken);
        var values = table.Select(row => $"{row.Values[0]}:{row.Values[1]}").Order().ToArray();

        CollectionAssert.AreEqual(new[] { "BERLIN:N", "PARIS:Y", "WARSAW:Y" }, values);
    }
}
