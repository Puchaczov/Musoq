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
        const string query = (
            "\n"
            + "            SELECT a.City,\n"
            + "                   CASE\n"
            + "                       WHEN EXISTS (\n"
            + "                           SELECT b.City FROM #B.entities() b\n"
            + "                           WHERE b.Country = a.Country\n"
            + "                       )\n"
            + "                       THEN 'Y'\n"
            + "                       ELSE 'N'\n"
            + "                   END AS HasMatch\n"
            + "            FROM #A.entities() a\n"
            + "            ORDER BY a.City"
        );

        var table = CreateAndRunVirtualMachine(query, CreatePredicateContextSources())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("HasMatch", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["BERLIN", "N"], ["PARIS", "Y"], ["WARSAW", "Y"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("-> Apply", inspection.PlanningText);
        Assert.IsFalse(
            inspection.PhysicalPlanText.Contains("PhysicalHashJoin [LeftSemi]", System.StringComparison.Ordinal)
        );
    }

    [TestMethod]
    public void WhenInSubquery_IsUsedInJoinCondition_ShouldEvaluateWithoutFilteringLeftSourceEarly()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, b.City\n"
            + "            FROM #A.entities() a\n"
            + "            INNER JOIN #B.entities() b\n"
            + "                ON a.Country = b.Country\n"
            + "               AND a.City IN (\n"
            + "                   SELECT c.City FROM #C.entities() c\n"
            + "               )\n"
            + "            ORDER BY a.City, b.City"
        );

        var table = CreateAndRunVirtualMachine(query, CreatePredicateContextSources())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["PARIS", "LYON"],
            ["PARIS", "PARIS"],
            ["WARSAW", "KRAKOW"],
            ["WARSAW", "WARSAW"]
        );

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("-> Apply", inspection.PlanningText);
    }

    [TestMethod]
    public void WhenExistsSubquery_IsUsedInQualify_ShouldFilterAfterWindowing()
    {
        const string query = (
            "\n"
            + "            SELECT a.City,\n"
            + "                   RowNumber() OVER (PARTITION BY a.Country ORDER BY a.Population DESC) AS RankInCountry\n"
            + "            FROM #A.entities() a\n"
            + "            QUALIFY RowNumber() OVER (PARTITION BY a.Country ORDER BY a.Population DESC) = 1\n"
            + "                AND EXISTS (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.City = a.City\n"
            + "            )\n"
            + "            ORDER BY a.City"
        );

        var table = CreateAndRunVirtualMachine(query, CreatePredicateContextSources())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("RankInCountry", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["PARIS", 1L], ["WARSAW", 1L]);
    }

    [TestMethod]
    public void WhenExistsSubquery_IsUsedInHaving_ShouldFilterGroups()
    {
        const string query = (
            "\n"
            + "            SELECT a.Country, Count(a.City) AS CityCount\n"
            + "            FROM #A.entities() a\n"
            + "            GROUP BY a.Country\n"
            + "            HAVING EXISTS (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "            )\n"
            + "            ORDER BY a.Country"
        );

        var table = CreateAndRunVirtualMachine(query, CreatePredicateContextSources())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)), ("CityCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["FRANCE", 1L], ["POLAND", 1L]);
    }

    [TestMethod]
    public void WhenHavingPredicateSubqueryReferencesNonGroupedRowValue_ShouldReject()
    {
        const string query = (
            "\n"
            + "            SELECT a.Country, Count(a.City) AS CityCount\n"
            + "            FROM #A.entities() a\n"
            + "            GROUP BY a.Country\n"
            + "            HAVING EXISTS (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.City = a.City\n"
            + "            )"
        );

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreatePredicateContextSources())
        );

        Assert.IsTrue(
            exception.Envelopes.Any(envelope => envelope.Code == DiagnosticCode.MQ2024_InvalidSubquery),
            $"Expected MQ2024, got {string.Join(", ", exception.Envelopes.Select(envelope => envelope.Code))}."
        );
        StringAssert.Contains(exception.Message, "HAVING");
        StringAssert.Contains(exception.Message, "grouping keys");
        StringAssert.Contains(exception.Message, "non-grouped row values");
    }

    [TestMethod]
    public void WhenQuantifiedSubquery_IsUsedInSelectExpression_ShouldEvaluateBooleanResult()
    {
        const string query = (
            "\n"
            + "            SELECT a.City,\n"
            + "                   a.Country = ANY (\n"
            + "                       SELECT b.Country FROM #B.entities() b\n"
            + "                   ) AS BiggerThanAny\n"
            + "            FROM #A.entities() a\n"
            + "            ORDER BY a.City"
        );

        var table = CreateAndRunVirtualMachine(query, CreatePredicateContextSources())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("BiggerThanAny", typeof(bool?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["BERLIN", false], ["PARIS", true], ["WARSAW", true]);
    }

    [TestMethod]
    public void WhenNegatedUncorrelatedExistsSubquery_IsUsedInSelectExpression_ShouldEvaluateBooleanResult()
    {
        const string query = (
            "\n"
            + "            SELECT a.City,\n"
            + "                   NOT EXISTS (\n"
            + "                       SELECT b.City FROM #B.entities() b\n"
            + "                       WHERE b.Country = 'SPAIN'\n"
            + "                   ) AS MissingSpain\n"
            + "            FROM #A.entities() a\n"
            + "            ORDER BY a.City"
        );

        var table = CreateAndRunVirtualMachine(query, CreatePredicateContextSources())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("MissingSpain", typeof(bool)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["BERLIN", true], ["PARIS", true], ["WARSAW", true]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftMark]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1._sq_1_key IS NULL as MissingSpain", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenNegatedCorrelatedExistsSubquery_IsUsedInSelectExpression_ShouldUseEqualityFallback()
    {
        const string query = (
            "\n"
            + "            SELECT a.City,\n"
            + "                   NOT EXISTS (\n"
            + "                       SELECT b.City FROM #B.entities() b\n"
            + "                       WHERE b.Country = a.Country\n"
            + "                   ) AS HasNoCountryMatch\n"
            + "            FROM #A.entities() a\n"
            + "            ORDER BY a.City"
        );

        var table = CreateAndRunVirtualMachine(query, CreatePredicateContextSources())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("HasNoCountryMatch", typeof(bool))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["BERLIN", true], ["PARIS", false], ["WARSAW", false]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftMark]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1._sq_1_key IS NULL as HasNoCountryMatch", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenPartitionedRangeQuantifiedSubquery_IsUsedInSelectExpression_ShouldUseThreeValuedTypedFallback()
    {
        const string query = (
            "\n"
            + "            SELECT a.City,\n"
            + "                   a.Population > ANY (\n"
            + "                       SELECT b.Population FROM #B.entities() b\n"
            + "                       WHERE b.Country = a.Country\n"
            + "                   ) AS BiggerThanAny\n"
            + "            FROM #A.entities() a\n"
            + "            ORDER BY a.City"
        );

        var table = CreateAndRunVirtualMachine(query, CreatePredicateContextSources())
            .Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("BiggerThanAny", typeof(bool?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["BERLIN", false], ["PARIS", false], ["WARSAW", true]);
        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("-> Apply", inspection.PlanningText);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreatePredicateContextSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity
                    {
                        City = "WARSAW",
                        Country = "POLAND",
                        Population = 500m,
                    },
                    new BasicEntity
                    {
                        City = "BERLIN",
                        Country = "GERMANY",
                        Population = 250m,
                    },
                    new BasicEntity
                    {
                        City = "PARIS",
                        Country = "FRANCE",
                        Population = 300m,
                    },
                ]
            },
            {
                "#B",
                [
                    new BasicEntity
                    {
                        City = "WARSAW",
                        Country = "POLAND",
                        Population = 100m,
                    },
                    new BasicEntity
                    {
                        City = "KRAKOW",
                        Country = "POLAND",
                        Population = 120m,
                    },
                    new BasicEntity
                    {
                        City = "PARIS",
                        Country = "FRANCE",
                        Population = 450m,
                    },
                    new BasicEntity
                    {
                        City = "LYON",
                        Country = "FRANCE",
                        Population = 350m,
                    },
                ]
            },
            {
                "#C",
                [
                    new BasicEntity
                    {
                        City = "WARSAW",
                        Country = "POLAND",
                        Population = 10m,
                    },
                    new BasicEntity
                    {
                        City = "PARIS",
                        Country = "FRANCE",
                        Population = 20m,
                    },
                ]
            },
        };
    }
}
