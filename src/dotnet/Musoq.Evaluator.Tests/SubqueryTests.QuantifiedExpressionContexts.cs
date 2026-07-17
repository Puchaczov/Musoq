using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenCorrelatedAnyAndAllAreProjected_ShouldPreserveNullAndEmptyGroupSemantics()
    {
        const string query = @"
            SELECT a.Name,
                   a.NullableValue > ANY (
                       SELECT b.NullableValue FROM #B.entities() b
                       WHERE b.Country = a.Country
                   ) AS AnyGreater,
                   a.NullableValue > ALL (
                       SELECT b.NullableValue FROM #B.entities() b
                       WHERE b.Country = a.Country
                   ) AS AllGreater
            FROM #A.entities() a
            ORDER BY a.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "Empty", Country = "FR", NullableValue = 10 },
                    new BasicEntity { Name = "High", Country = "PL", NullableValue = 10 },
                    new BasicEntity { Name = "Low", Country = "PL", NullableValue = 1 },
                    new BasicEntity { Name = "NullInnerOnly", Country = "DE", NullableValue = 10 },
                    new BasicEntity { Name = "NullOuter", Country = "PL", NullableValue = null }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "PL-Value", Country = "PL", NullableValue = 5 },
                    new BasicEntity { Name = "PL-Null", Country = "PL", NullableValue = null },
                    new BasicEntity { Name = "DE-Null", Country = "DE", NullableValue = null }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("AnyGreater", typeof(bool)),
            ("AllGreater", typeof(bool)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Empty", false, true],
            ["High", true, false],
            ["Low", false, false],
            ["NullInnerOnly", false, false],
            ["NullOuter", false, false]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalSortMergeJoin [LeftMark]", inspection.PhysicalPlanText);
        Assert.Contains("PredicateRangeMark", inspection.PlanningText);
        AssertNoPerRowSubqueryExecution(inspection);
    }
}
