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
    public void WhenExistsSubquery_IsUsedInCaseExpression_ShouldEvaluatePerOuterRow()
    {
        const string query = @"
            SELECT a.City,
                   CASE
                       WHEN EXISTS (
                           SELECT b.City FROM #B.entities() b
                           WHERE b.Country = a.Country
                       )
                       THEN 'Y'
                       ELSE 'N'
                   END AS HasMatch
            FROM #A.entities() a
            ORDER BY a.City";

        var table = CreateAndRunVirtualMachine(query, CreatePredicateContextSources()).Run(TestContext.CancellationToken);

        CollectionAssert.AreEqual(
            new[] { "BERLIN:N", "PARIS:Y", "WARSAW:Y" },
            table.Select(row => $"{row.Values[0]}:{row.Values[1]}").ToArray());

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftOuter]", inspection.PhysicalPlanText);
        Assert.IsFalse(inspection.PhysicalPlanText.Contains("PhysicalHashJoin [LeftSemi]", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void WhenInSubquery_IsUsedInJoinCondition_ShouldEvaluateWithoutFilteringLeftSourceEarly()
    {
        const string query = @"
            SELECT a.City, b.City
            FROM #A.entities() a
            INNER JOIN #B.entities() b
                ON a.Country = b.Country
               AND a.City IN (
                   SELECT c.City FROM #C.entities() c
               )
            ORDER BY a.City, b.City";

        var table = CreateAndRunVirtualMachine(query, CreatePredicateContextSources()).Run(TestContext.CancellationToken);

        CollectionAssert.AreEqual(
            new[] { "PARIS:LYON", "PARIS:PARIS", "WARSAW:KRAKOW", "WARSAW:WARSAW" },
            table.Select(row => $"{row.Values[0]}:{row.Values[1]}").ToArray());

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftOuter]", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenExistsSubquery_IsUsedInQualify_ShouldFilterAfterWindowing()
    {
        const string query = @"
            SELECT a.City,
                   RowNumber() OVER (PARTITION BY a.Country ORDER BY a.Population DESC) AS RankInCountry
            FROM #A.entities() a
            QUALIFY RowNumber() OVER (PARTITION BY a.Country ORDER BY a.Population DESC) = 1
                AND EXISTS (
                SELECT b.City FROM #B.entities() b
                WHERE b.City = a.City
            )
            ORDER BY a.City";

        var table = CreateAndRunVirtualMachine(query, CreatePredicateContextSources()).Run(TestContext.CancellationToken);

        CollectionAssert.AreEqual(
            new[] { "PARIS:1", "WARSAW:1" },
            table.Select(row => $"{row.Values[0]}:{System.Convert.ToInt64(row.Values[1])}").ToArray());
    }

    [TestMethod]
    public void WhenExistsSubquery_IsUsedInHaving_ShouldFilterGroups()
    {
        const string query = @"
            SELECT a.Country, Count(a.City) AS CityCount
            FROM #A.entities() a
            GROUP BY a.Country
            HAVING EXISTS (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
            )
            ORDER BY a.Country";

        var table = CreateAndRunVirtualMachine(query, CreatePredicateContextSources()).Run(TestContext.CancellationToken);

        CollectionAssert.AreEqual(
            new[] { "FRANCE:1", "POLAND:1" },
            table.Select(row => $"{row.Values[0]}:{System.Convert.ToInt64(row.Values[1])}").ToArray());
    }

    [TestMethod]
    public void WhenHavingPredicateSubqueryReferencesNonGroupedRowValue_ShouldReject()
    {
        const string query = @"
            SELECT a.Country, Count(a.City) AS CityCount
            FROM #A.entities() a
            GROUP BY a.Country
            HAVING EXISTS (
                SELECT b.City FROM #B.entities() b
                WHERE b.City = a.City
            )";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreatePredicateContextSources()));

        Assert.IsTrue(
            exception.Envelopes.Any(envelope => envelope.Code == DiagnosticCode.MQ2024_InvalidSubquery),
            $"Expected MQ2024, got {string.Join(", ", exception.Envelopes.Select(envelope => envelope.Code))}.");
        StringAssert.Contains(exception.Message, "HAVING");
        StringAssert.Contains(exception.Message, "grouping keys");
        StringAssert.Contains(exception.Message, "non-grouped row values");
    }

    [TestMethod]
    public void WhenQuantifiedSubquery_IsUsedInSelectExpression_ShouldEvaluateBooleanResult()
    {
        const string query = @"
            SELECT a.City,
                   a.Country = ANY (
                       SELECT b.Country FROM #B.entities() b
                   ) AS BiggerThanAny
            FROM #A.entities() a
            ORDER BY a.City";

        var table = CreateAndRunVirtualMachine(query, CreatePredicateContextSources()).Run(TestContext.CancellationToken);

        var actual = table.Select(row => $"{row.Values[0]}:{row.Values[1]}").ToArray();
        CollectionAssert.AreEqual(
            new[] { "BERLIN:False", "PARIS:True", "WARSAW:True" },
            actual,
            string.Join(", ", actual));
    }

    [TestMethod]
    public void WhenNegatedUncorrelatedExistsSubquery_IsUsedInSelectExpression_ShouldEvaluateBooleanResult()
    {
        const string query = @"
            SELECT a.City,
                   NOT EXISTS (
                       SELECT b.City FROM #B.entities() b
                       WHERE b.Country = 'SPAIN'
                   ) AS MissingSpain
            FROM #A.entities() a
            ORDER BY a.City";

        var table = CreateAndRunVirtualMachine(query, CreatePredicateContextSources()).Run(TestContext.CancellationToken);

        var actual = table.Select(row => $"{row.Values[0]}:{row.Values[1]}").ToArray();
        CollectionAssert.AreEqual(
            new[] { "BERLIN:True", "PARIS:True", "WARSAW:True" },
            actual,
            string.Join(", ", actual));

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalNestedLoopJoin [LeftOuter]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1._sq_1_key IS NULL as MissingSpain", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenNegatedCorrelatedExistsSubquery_IsUsedInSelectExpression_ShouldUseEqualityFallback()
    {
        const string query = @"
            SELECT a.City,
                   NOT EXISTS (
                       SELECT b.City FROM #B.entities() b
                       WHERE b.Country = a.Country
                   ) AS HasNoCountryMatch
            FROM #A.entities() a
            ORDER BY a.City";

        var table = CreateAndRunVirtualMachine(query, CreatePredicateContextSources()).Run(TestContext.CancellationToken);

        var actual = table.Select(row => $"{row.Values[0]}:{row.Values[1]}").ToArray();
        CollectionAssert.AreEqual(
            new[] { "BERLIN:True", "PARIS:False", "WARSAW:False" },
            actual,
            string.Join(", ", actual));

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftOuter]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1._sq_1_key IS NULL as HasNoCountryMatch", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenNonEqualityQuantifiedSubquery_IsUsedInSelectExpression_ShouldRejectUnsafeFallback()
    {
        const string query = @"
            SELECT a.City,
                   a.Population > ANY (
                       SELECT b.Population FROM #B.entities() b
                       WHERE b.Country = a.Country
                   ) AS BiggerThanAny
            FROM #A.entities() a
            ORDER BY a.City";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreatePredicateContextSources()));

        Assert.IsTrue(
            exception.Envelopes.Any(envelope => envelope.Code == DiagnosticCode.MQ2024_InvalidSubquery),
            $"Expected MQ2024, got {string.Join(", ", exception.Envelopes.Select(envelope => envelope.Code))}.");
        StringAssert.Contains(exception.Message, "equality-only correlation");
        StringAssert.Contains(exception.Message, "APPLY lowering");
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreatePredicateContextSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { City = "WARSAW", Country = "POLAND", Population = 500m },
                    new BasicEntity { City = "BERLIN", Country = "GERMANY", Population = 250m },
                    new BasicEntity { City = "PARIS", Country = "FRANCE", Population = 300m }
                ]
            },
            {
                "#B", [
                    new BasicEntity { City = "WARSAW", Country = "POLAND", Population = 100m },
                    new BasicEntity { City = "KRAKOW", Country = "POLAND", Population = 120m },
                    new BasicEntity { City = "PARIS", Country = "FRANCE", Population = 450m },
                    new BasicEntity { City = "LYON", Country = "FRANCE", Population = 350m }
                ]
            },
            {
                "#C", [
                    new BasicEntity { City = "WARSAW", Country = "POLAND", Population = 10m },
                    new BasicEntity { City = "PARIS", Country = "FRANCE", Population = 20m }
                ]
            }
        };
    }
}
