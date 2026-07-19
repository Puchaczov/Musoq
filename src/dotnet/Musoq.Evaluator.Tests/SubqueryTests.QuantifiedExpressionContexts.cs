using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    [DataRow("=")]
    [DataRow(">")]
    [DataRow(">=")]
    [DataRow("<=")]
    public void WhenCorrelatedSomeUsesComparison_ShouldFilterWithExactThreeValuedSemantics(string comparison)
    {
        var query = $@"
            SELECT a.Name
            FROM #A.entities() a
            WHERE a.NullableValue {comparison} SOME (
                SELECT b.NullableValue FROM #B.entities() b
                WHERE b.Country = a.Country
            )
            ORDER BY a.Name";
        var table = CreateAndRunVirtualMachine(query, CreateQuantifiedBoundarySources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ExpectedSomeRows(comparison));

        var inspection = CompileSubqueryForInspection(query);
        if (comparison == "=")
            Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        else
            Assert.Contains("PredicateRangeSemiJoin", inspection.PlanningText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenCorrelatedInAndNotInAreProjectedThroughCase_ShouldPreserveNullSemantics()
    {
        const string query = @"
            SELECT a.Name,
                   CASE WHEN a.NullableValue IN (
                       SELECT b.NullableValue FROM #B.entities() b
                       WHERE b.Country = a.Country
                   ) THEN 'Y' ELSE 'N' END AS InResult,
                   CASE WHEN a.NullableValue NOT IN (
                       SELECT b.NullableValue FROM #B.entities() b
                       WHERE b.Country = a.Country
                   ) THEN 'Y' ELSE 'N' END AS NotInResult
            FROM #A.entities() a
            ORDER BY a.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Name = "Empty", Country = "FR", NullableValue = 1 },
                new BasicEntity { Name = "Match", Country = "PL", NullableValue = 5 },
                new BasicEntity { Name = "NoMatch", Country = "PL", NullableValue = 10 },
                new BasicEntity { Name = "NullOnly", Country = "DE", NullableValue = 1 },
                new BasicEntity { Name = "NullOuter", Country = "PL", NullableValue = null }
            ],
            ["#B"] =
            [
                new BasicEntity { Name = "PL-Match", Country = "PL", NullableValue = 5 },
                new BasicEntity { Name = "PL-Null", Country = "PL", NullableValue = null },
                new BasicEntity { Name = "DE-Null", Country = "DE", NullableValue = null }
            ]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("InResult", typeof(string)),
            ("NotInResult", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Empty", "N", "Y"],
            ["Match", "Y", "N"],
            ["NoMatch", "N", "N"],
            ["NullOnly", "N", "N"],
            ["NullOuter", "N", "N"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PredicateHashMark", inspection.PlanningText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenCorrelatedNotInIsUsedAsFilter_ShouldRejectMatchesNullsAndOuterNulls()
    {
        const string query = @"
            SELECT a.Name
            FROM #A.entities() a
            WHERE a.NullableValue NOT IN (
                SELECT b.NullableValue FROM #B.entities() b
                WHERE b.Country = a.Country
            )
            ORDER BY a.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Name = "Empty", Country = "FR", NullableValue = 1 },
                new BasicEntity { Name = "Match", Country = "PL", NullableValue = 5 },
                new BasicEntity { Name = "NoMatch", Country = "PL", NullableValue = 10 },
                new BasicEntity { Name = "NullOnly", Country = "DE", NullableValue = 1 },
                new BasicEntity { Name = "NullOuter", Country = "PL", NullableValue = null }
            ],
            ["#B"] =
            [
                new BasicEntity { Name = "PL-Match", Country = "PL", NullableValue = 5 },
                new BasicEntity { Name = "PL-Null", Country = "PL", NullableValue = null },
                new BasicEntity { Name = "DE-Null", Country = "DE", NullableValue = null }
            ]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Empty"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void WhenCorrelatedAnyIsUsedInHaving_ShouldFilterGroupedRowsSetWise()
    {
        const string query = @"
            SELECT a.Country, Sum(a.Population) AS Total
            FROM #A.entities() a
            GROUP BY a.Country
            HAVING a.Country = ANY (
                SELECT b.Country FROM #B.entities() b
                WHERE b.Country = a.Country
            )
            ORDER BY a.Country";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Country = "DE", Population = 10m },
                new BasicEntity { Country = "FR", Population = 10m },
                new BasicEntity { Country = "PL", Population = 100m }
            ],
            ["#B"] =
            [
                new BasicEntity { Country = "DE", Population = 20m },
                new BasicEntity { Country = "PL", Population = 50m }
            ]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Country", typeof(string)),
            ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["DE", 10m],
            ["PL", 100m]);
        AssertNoPerRowSubqueryExecution(CompileSubqueryForInspection(query));
    }

    private static object?[][] ExpectedSomeRows(string comparison)
    {
        return comparison switch
        {
            "=" => [["Equal"]],
            ">" => [["High"]],
            ">=" => [["Equal"], ["High"]],
            "<=" => [["Equal"], ["Low"]],
            _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, null)
        };
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateQuantifiedBoundarySources() => new()
    {
        ["#A"] =
        [
            new BasicEntity { Name = "Empty", Country = "FR", NullableValue = 10 },
            new BasicEntity { Name = "Equal", Country = "PL", NullableValue = 5 },
            new BasicEntity { Name = "High", Country = "PL", NullableValue = 10 },
            new BasicEntity { Name = "Low", Country = "PL", NullableValue = 1 },
            new BasicEntity { Name = "NullInnerOnly", Country = "DE", NullableValue = 10 },
            new BasicEntity { Name = "NullOuter", Country = "PL", NullableValue = null }
        ],
        ["#B"] =
        [
            new BasicEntity { Name = "PL-Value", Country = "PL", NullableValue = 5 },
            new BasicEntity { Name = "PL-Null", Country = "PL", NullableValue = null },
            new BasicEntity { Name = "DE-Null", Country = "DE", NullableValue = null }
        ]
    };

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
