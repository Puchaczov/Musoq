using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenScalarSubquery_HasEqualityCorrelation_ShouldUseGroupedAggregateAndLeftJoin()
    {
        const string query = @"
            SELECT a.City, (
                SELECT b.City FROM #C.entities() b
                WHERE b.Country = a.Country
            ) AS MatchCity
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        CollectionAssert.AreEqual(
            new object?[] { "KRAKOW", null, "PARIS" },
            table.Select(row => row.Values[1]).ToArray());

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftOuter]", inspection.PhysicalPlanText);
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

        Assert.AreEqual(3, table.Count);
        CollectionAssert.AreEqual(
            new object?[] { 210m, null, 450m },
            table.Select(row => row.Values[1]).ToArray());

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftOuter]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1_corr_0", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_HasOrderByAndTake_ShouldExplainUnsupportedApplyFallback()
    {
        const string query = @"
            SELECT a.City, (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
                ORDER BY b.Population DESC
                TAKE 1
            ) AS MatchCity
            FROM #A.entities() a";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateScalarSources()));

        Assert.IsTrue(exception.Envelopes.Any(envelope => envelope.Code == DiagnosticCode.MQ2024_InvalidSubquery));
        StringAssert.Contains(exception.Message, "Correlated scalar subqueries");
        StringAssert.Contains(exception.Message, "ORDER BY");
        StringAssert.Contains(exception.Message, "APPLY fallback");
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

        Assert.AreEqual(2, table.Count);
        CollectionAssert.AreEqual(
            new[] { "WARSAW", "PARIS" },
            table.Select(row => (string)row.Values[0]).ToArray());

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("CtePhase [cte2]", inspection.ExecutionPlanText);
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

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.All(row => (string)row.Values[0] == "yes"));
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

        CollectionAssert.AreEqual(
            new[] { "PARIS", "WARSAW", "BERLIN" },
            table.Select(row => (string)row.Values[0]).ToArray());
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

        Assert.AreEqual(3, table.Count);
        CollectionAssert.AreEqual(
            new object?[] { "KRAKOW", null, "PARIS" },
            table.Select(row => row.Values[1]).ToArray());
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

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("PARIS", table[0].Values[0]);
        Assert.AreEqual(3, Convert.ToInt32(table[0].Values[1]));
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

        CollectionAssert.AreEqual(
            new[] { "FRANCE", "GERMANY", "POLAND" },
            table.Select(row => (string)row.Values[0]).OrderBy(item => item).ToArray());
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

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("BERLIN", table[0].Values[0]);
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

        var ordered = table.OrderBy(row => Convert.ToInt64(row.Values[1])).ToArray();
        Assert.AreEqual("PARIS", ordered[0].Values[0]);
        Assert.AreEqual("WARSAW", ordered[1].Values[0]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftOuter]", inspection.PhysicalPlanText);
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
