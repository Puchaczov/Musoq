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
    public void Characterization_CteBodyPredicateApply_ShouldUseCteLocalCorrelationOnly()
    {
        const string query = (
            "\n"
            + "            WITH matched AS (\n"
            + "                SELECT a.City, a.Country,\n"
            + "                       CASE\n"
            + "                           WHEN EXISTS (\n"
            + "                               SELECT b.City FROM #B.entities() b\n"
            + "                               WHERE b.Country = a.Country\n"
            + "                           )\n"
            + "                           THEN 'Y'\n"
            + "                           ELSE 'N'\n"
            + "                       END AS HasMatch\n"
            + "                FROM #A.entities() a\n"
            + "            )\n"
            + "            SELECT m.City, m.HasMatch\n"
            + "            FROM matched m\n"
            + "            ORDER BY m.City"
        );

        var table = CreateAndRunVirtualMachine(query, CreateCharacterizationSources())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("HasMatch", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["BERLIN", "Y"], ["PARIS", "Y"], ["WARSAW", "Y"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("-> Apply", inspection.PlanningText);
    }

    [TestMethod]
    public void Characterization_CorrelatedScalarAggregateInJoinOn_ShouldExecuteOnlyWhenJoinPredicateDemandsIt()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, b.City\n"
            + "            FROM #A.entities() a\n"
            + "            INNER JOIN #B.entities() b\n"
            + "                ON a.Country = b.Country\n"
            + "               AND b.Population = (\n"
            + "                   SELECT Max(c.Population) FROM #C.entities() c\n"
            + "                   WHERE c.Country = a.Country\n"
            + "               )\n"
            + "            ORDER BY a.City, b.City"
        );

        var table = CreateAndRunVirtualMachine(query, CreateCharacterizationSources())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["BERLIN", "MUNICH"], ["WARSAW", "KRAKOW"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("-> Apply", inspection.PlanningText);
    }

    [TestMethod]
    public void Characterization_CorrelatedAllQuantifier_ShouldExposeAntiSemiJoinStrategy()
    {
        const string query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.Population > ALL (\n"
            + "                SELECT b.Population FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "            )\n"
            + "            ORDER BY a.City"
        );

        var table = CreateAndRunVirtualMachine(query, CreateCharacterizationSources())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("-> PredicateAntiSemiJoin", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void Characterization_CorrelatedApplySetOperatorDerivedTable_ShouldExposeSingleDerivedJoinStrategy()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, d.City\n"
            + "            FROM #A.entities() a\n"
            + "            CROSS APPLY (\n"
            + "                SELECT b.City, b.Country FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "                UNION (City, Country)\n"
            + "                SELECT c.City, c.Country FROM #C.entities() c\n"
            + "                WHERE c.Country = a.Country\n"
            + "            ) d\n"
            + "            ORDER BY a.City, d.City"
        );

        var table = CreateAndRunVirtualMachine(query, CreateCharacterizationSources())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["BERLIN", "MUNICH"],
            ["PARIS", "LYON"],
            ["PARIS", "PARIS"],
            ["WARSAW", "GDANSK"],
            ["WARSAW", "KRAKOW"],
            ["WARSAW", "WARSAW"]
        );

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains(
            "SubqueryStrategy [SubqueryLoweringStrategy] _dt_1 -> DerivedTableJoin",
            inspection.PlanningText
        );
        Assert.Contains("PhysicalHashJoin [Inner]", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void Characterization_UserCteNamedLikeGeneratedDerivedTable_ShouldNotCollide()
    {
        const string query = (
            "\n"
            + "            WITH _dt_1 AS (\n"
            + "                SELECT a.City FROM #A.entities() a\n"
            + "            )\n"
            + "            SELECT a.City, d.City\n"
            + "            FROM #A.entities() a\n"
            + "            CROSS APPLY (\n"
            + "                SELECT b.City, b.Country FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "            ) d\n"
            + "            ORDER BY a.City, d.City"
        );

        var table = CreateAndRunVirtualMachine(query, CreateCharacterizationSources())
            .Run(TestContext.CancellationToken);
        var inspection = CompileSubqueryForInspection(query);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["BERLIN", "MUNICH"],
            ["PARIS", "LYON"],
            ["WARSAW", "GDANSK"],
            ["WARSAW", "KRAKOW"]
        );
        Assert.Contains(
            "SubqueryStrategy [SubqueryLoweringStrategy] _dt_2 -> DerivedTableJoin",
            inspection.PlanningText
        );
    }

    [TestMethod]
    public void Characterization_UserCteNamedLikeGeneratedPredicateSubquery_ShouldNotCollide()
    {
        const string query = (
            "\n"
            + "            WITH _sq_1 AS (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "            )\n"
            + "            SELECT a.City\n"
            + "            FROM #A.entities() a\n"
            + "            WHERE a.Country IN (\n"
            + "                SELECT b.Country FROM #B.entities() b\n"
            + "            )\n"
            + "            ORDER BY a.City"
        );

        var table = CreateAndRunVirtualMachine(query, CreateCharacterizationSources())
            .Run(TestContext.CancellationToken);
        var inspection = CompileSubqueryForInspection(query);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["BERLIN"], ["PARIS"], ["WARSAW"]);
        Assert.Contains(
            "SubqueryStrategy [SubqueryLoweringStrategy] _sq_2 -> PredicateSemiJoin",
            inspection.PlanningText
        );
    }

    [TestMethod]
    public void Characterization_UserCteNamedLikeGeneratedScalarMaterialization_ShouldNotCollide()
    {
        const string query = (
            "\n"
            + "            WITH _sm_1 AS (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "            )\n"
            + "            SELECT (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                ORDER BY b.Population DESC\n"
            + "                TAKE 1\n"
            + "            ) AS TopCity\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateCharacterizationSources())
            .Run(TestContext.CancellationToken);
        var inspection = CompileSubqueryForInspection(query);

        TableMaterializationTestHelper.AssertColumns(table, ("TopCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["LYON"], ["LYON"], ["LYON"]);
        Assert.Contains("_sm_2", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void Characterization_TopLevelCteDefinitionConsumingQueryAlias_ShouldRejectWithReadableAliasMessage()
    {
        const string query = (
            "\n"
            + "            WITH bad AS (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "            )\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.City IN (\n"
            + "                SELECT x.City FROM bad x\n"
            + "            )"
        );

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateCharacterizationSources())
        );

        Assert.IsTrue(
            exception.Envelopes.Any(envelope => envelope.Code == DiagnosticCode.MQ3015_UnknownAlias),
            $"Expected MQ3015, got {string.Join(", ", exception.Envelopes.Select(envelope => envelope.Code))}."
        );
        StringAssert.Contains(exception.Message, "Unknown alias");
        StringAssert.Contains(exception.Message, "a");
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateCharacterizationSources()
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
                        City = "KRAKOW",
                        Country = "POLAND",
                        Population = 220m,
                    },
                    new BasicEntity
                    {
                        City = "GDANSK",
                        Country = "POLAND",
                        Population = 120m,
                    },
                    new BasicEntity
                    {
                        City = "MUNICH",
                        Country = "GERMANY",
                        Population = 250m,
                    },
                    new BasicEntity
                    {
                        City = "LYON",
                        Country = "FRANCE",
                        Population = 450m,
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
                        Population = 220m,
                    },
                    new BasicEntity
                    {
                        City = "GDANSK",
                        Country = "POLAND",
                        Population = 50m,
                    },
                    new BasicEntity
                    {
                        City = "MUNICH",
                        Country = "GERMANY",
                        Population = 250m,
                    },
                    new BasicEntity
                    {
                        City = "PARIS",
                        Country = "FRANCE",
                        Population = 999m,
                    },
                ]
            },
        };
    }
}
