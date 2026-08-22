using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenScalarSubquery_HasEqualityCorrelation_ShouldUseGroupedAggregateAndHashSingle()
    {
        const string query = @"
            SELECT a.City, (
                SELECT b.City FROM #C.entities() b
                WHERE b.Country = a.Country
            ) AS MatchCity
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("MatchCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "KRAKOW"],
            new object?[] { "BERLIN", null },
            ["PARIS", "PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSingle]", inspection.PhysicalPlanText);
        Assert.Contains("-> ScalarHashSingle", inspection.PlanningText);
        Assert.Contains("_sq_1_corr_0", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasCorrelatedAggregate_ShouldGroupByCorrelationKey()
    {
        const string query = @"
            SELECT a.City, (
                SELECT Sum(b.Population) FROM #B.entities() b
                WHERE b.Country = a.Country
            ) AS TotalPopulation
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("TotalPopulation", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", 210m],
            new object?[] { "BERLIN", null },
            ["PARIS", 450m]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSingle]", inspection.PhysicalPlanText);
        Assert.Contains("-> ScalarHashSingle", inspection.PlanningText);
        Assert.Contains("_sq_1_corr_0", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_HasOrderByAndTake_ShouldApplyLimitPerCorrelationKey()
    {
        const string query = @"
            SELECT a.City, (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
                ORDER BY b.Population DESC
                TAKE 1
            ) AS MatchCity
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("MatchCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "GDANSK"],
            new object?[] { "BERLIN", null },
            ["PARIS", "PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("WindowStrategy [WindowMaterialization] window -> MaterializeInput", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftSingle]", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenScalarSubquery_IsInJoinOn_ShouldParticipateInJoinPredicate()
    {
        const string query = @"
            SELECT a.City, b.City FROM #A.entities() a
            INNER JOIN #B.entities() b ON b.City = (
                SELECT c.City FROM #C.entities() c
                WHERE c.Country = a.Country
            )";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("b.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "KRAKOW"],
            ["PARIS", "PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhaseBoundary [Begin:cte2]", inspection.ExecutionPlanText);
        Assert.IsFalse(
            inspection.ExecutionPlanText.Contains("StoreTable [statement1 -> _tableResults[2]]", StringComparison.Ordinal),
            inspection.ExecutionPlanText);
        Assert.IsFalse(
            inspection.GeneratedCSharpCode.Contains("_tableResults[2]", StringComparison.Ordinal),
            inspection.GeneratedCSharpCode);
        Assert.Contains("a_sq_1Hash", inspection.GeneratedCSharpCode);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(a_sq_1.a_City, b.City));", inspection.GeneratedCSharpCode);
        Assert.IsFalse(
            inspection.GeneratedCSharpCode.Contains("AppendHashJoinRows(bRows, a_sq_1Hash, result, token);", StringComparison.Ordinal),
            inspection.GeneratedCSharpCode);
    }

    [TestMethod]
    public void WhenScalarSubquery_IsInsideFunctionArgumentAndCase_ShouldRewriteNestedExpression()
    {
        const string query = @"
            SELECT CASE
                WHEN Substring((
                    SELECT b.City FROM #B.entities() b
                    WHERE b.Country = 'FRANCE'
                ), 0, 3) = 'PAR'
                THEN 'yes'
                ELSE 'no'
            END AS Verdict
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Verdict", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["yes"], ["yes"], ["yes"]);
    }

    [TestMethod]
    public void WhenScalarSubquery_IsInOrderBy_ShouldSortByScalarValue()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            ORDER BY (
                SELECT c.Population FROM #C.entities() c
                WHERE c.Country = a.Country
            ) DESC";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["PARIS"], ["WARSAW"], ["BERLIN"]);
    }

    [TestMethod]
    public void WhenScalarSubquery_IsInsideCteBody_ShouldRewriteAndExposeValue()
    {
        const string query = @"
            WITH matched AS (
                SELECT a.City, (
                    SELECT c.City FROM #C.entities() c
                    WHERE c.Country = a.Country
                ) AS MatchCity
                FROM #A.entities() a
            )
            SELECT m.City, m.MatchCity FROM matched m";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("m.City", typeof(string)),
            ("m.MatchCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "KRAKOW"],
            new object?[] { "BERLIN", null },
            ["PARIS", "PARIS"]);
    }

    [TestMethod]
    public void WhenScalarSubquery_IsInGroupBy_ShouldGroupByScalarValue()
    {
        const string query = @"
            SELECT (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = 'FRANCE'
            ) AS KeyCity, Count(a.City) AS Count
            FROM #A.entities() a
            GROUP BY (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = 'FRANCE'
            )";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("KeyCity", typeof(string)),
            ("Count", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["PARIS", 3L]);
    }

    [TestMethod]
    public void WhenScalarSubquery_IsInHaving_ShouldFilterGroups()
    {
        const string query = @"
            SELECT a.Country, Sum(a.Population) AS TotalPopulation
            FROM #A.entities() a
            GROUP BY a.Country
            HAVING Sum(a.Population) > (
                SELECT c.Population FROM #C.entities() c
                WHERE c.Country = 'POLAND'
            )";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Country", typeof(string)),
            ("TotalPopulation", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["POLAND", 500m],
            ["GERMANY", 250m],
            ["FRANCE", 300m]);
    }

    [TestMethod]
    public void WhenScalarSubquery_IsInQualify_ShouldFilterWindowedRows()
    {
        const string query = @"
            SELECT a.City, RowNumber() OVER (ORDER BY a.City) AS RowNo
            FROM #A.entities() a
            QUALIFY RowNumber() OVER (ORDER BY a.City) = (
                SELECT 1 FROM #C.entities() c
                WHERE c.Country = 'POLAND'
            )";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("RowNo", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["BERLIN", 1L]);
    }

    [TestMethod]
    public void WhenScalarSubquery_IsInNamedWindowSpecification_ShouldOrderByScalarValue()
    {
        const string query = @"
            SELECT a.City, RowNumber() OVER ranked AS RowNo
            FROM #A.entities() a
            WINDOW ranked AS (
                ORDER BY (
                    SELECT c.Population FROM #C.entities() c
                    WHERE c.Country = a.Country
                ) DESC
            )";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("RowNo", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["PARIS", 1L],
            ["WARSAW", 2L],
            ["BERLIN", 3L]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSingle]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1_value", inspection.PhysicalPlanText);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateScalarSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            },
            {
                "#B", [
                    new BasicEntity("KRAKOW", "POLAND", 100),
                    new BasicEntity("GDANSK", "POLAND", 110),
                    new BasicEntity("PARIS", "FRANCE", 450)
                ]
            },
            {
                "#C", [
                    new BasicEntity("KRAKOW", "POLAND", 10),
                    new BasicEntity("PARIS", "FRANCE", 20)
                ]
            }
        };
    }
}
