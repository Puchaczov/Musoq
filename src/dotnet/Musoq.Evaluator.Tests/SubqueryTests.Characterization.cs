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
        const string query = @"
            WITH matched AS (
                SELECT a.City, a.Country,
                       CASE
                           WHEN EXISTS (
                               SELECT b.City FROM #B.entities() b
                               WHERE b.Country = a.Country
                           )
                           THEN 'Y'
                           ELSE 'N'
                       END AS HasMatch
                FROM #A.entities() a
            )
            SELECT m.City, m.HasMatch
            FROM matched m
            ORDER BY m.City";

        var table = CreateAndRunVirtualMachine(query, CreateCharacterizationSources()).Run(TestContext.CancellationToken);

        CollectionAssert.AreEqual(
            new[] { "BERLIN:Y", "PARIS:Y", "WARSAW:Y" },
            table.Select(row => $"{row.Values[0]}:{row.Values[1]}").ToArray());

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("-> PredicateHashMark", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftMark]", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void Characterization_CorrelatedScalarAggregateInJoinOn_ShouldDecorrelateBeforeJoiningRightSource()
    {
        const string query = @"
            SELECT a.City, b.City
            FROM #A.entities() a
            INNER JOIN #B.entities() b
                ON a.Country = b.Country
               AND b.Population = (
                   SELECT Max(c.Population) FROM #C.entities() c
                   WHERE c.Country = a.Country
               )
            ORDER BY a.City, b.City";

        var table = CreateAndRunVirtualMachine(query, CreateCharacterizationSources()).Run(TestContext.CancellationToken);

        CollectionAssert.AreEqual(
            new[] { "BERLIN:MUNICH", "WARSAW:KRAKOW" },
            table.Select(row => $"{row.Values[0]}:{row.Values[1]}").ToArray());

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("-> ScalarHashSingle", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftSingle]", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void Characterization_CorrelatedAllQuantifier_ShouldExposeAntiSemiJoinStrategy()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.Population > ALL (
                SELECT b.Population FROM #B.entities() b
                WHERE b.Country = a.Country
            )
            ORDER BY a.City";

        var table = CreateAndRunVirtualMachine(query, CreateCharacterizationSources()).Run(TestContext.CancellationToken);

        CollectionAssert.AreEqual(
            new[] { "WARSAW" },
            table.Select(row => (string)row.Values[0]).ToArray());

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("-> PredicateAntiSemiJoin", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void Characterization_CorrelatedApplySetOperatorDerivedTable_ShouldExposeSingleDerivedJoinStrategy()
    {
        const string query = @"
            SELECT a.City, d.City
            FROM #A.entities() a
            CROSS APPLY (
                SELECT b.City, b.Country FROM #B.entities() b
                WHERE b.Country = a.Country
                UNION (City, Country)
                SELECT c.City, c.Country FROM #C.entities() c
                WHERE c.Country = a.Country
            ) d
            ORDER BY a.City, d.City";

        var table = CreateAndRunVirtualMachine(query, CreateCharacterizationSources()).Run(TestContext.CancellationToken);

        CollectionAssert.AreEqual(
            new[] { "BERLIN:MUNICH", "PARIS:LYON", "PARIS:PARIS", "WARSAW:GDANSK", "WARSAW:KRAKOW", "WARSAW:WARSAW" },
            table.Select(row => $"{row.Values[0]}:{row.Values[1]}").ToArray());

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("SubqueryStrategy [SubqueryLoweringStrategy] _dt_1 -> DerivedTableJoin", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [Inner]", inspection.PhysicalPlanText);
        AssertNoPerRowSubqueryExecution(inspection);
    }

    [TestMethod]
    public void Characterization_UserCteNamedLikeGeneratedDerivedTable_ShouldNotCollide()
    {
        const string query = @"
            WITH _dt_1 AS (
                SELECT a.City FROM #A.entities() a
            )
            SELECT a.City, d.City
            FROM #A.entities() a
            CROSS APPLY (
                SELECT b.City, b.Country FROM #B.entities() b
                WHERE b.Country = a.Country
            ) d
            ORDER BY a.City, d.City";

        var table = CreateAndRunVirtualMachine(query, CreateCharacterizationSources()).Run(TestContext.CancellationToken);
        var inspection = CompileSubqueryForInspection(query);

        CollectionAssert.AreEqual(
            new[] { "BERLIN:MUNICH", "PARIS:LYON", "WARSAW:GDANSK", "WARSAW:KRAKOW" },
            table.Select(row => $"{row.Values[0]}:{row.Values[1]}").ToArray());
        Assert.Contains("SubqueryStrategy [SubqueryLoweringStrategy] _dt_2 -> DerivedTableJoin", inspection.PlanningText);
    }

    [TestMethod]
    public void Characterization_UserCteNamedLikeGeneratedPredicateSubquery_ShouldNotCollide()
    {
        const string query = @"
            WITH _sq_1 AS (
                SELECT b.City FROM #B.entities() b
            )
            SELECT a.City
            FROM #A.entities() a
            WHERE a.Country IN (
                SELECT b.Country FROM #B.entities() b
            )
            ORDER BY a.City";

        var table = CreateAndRunVirtualMachine(query, CreateCharacterizationSources()).Run(TestContext.CancellationToken);
        var inspection = CompileSubqueryForInspection(query);

        CollectionAssert.AreEqual(
            new[] { "BERLIN", "PARIS", "WARSAW" },
            table.Select(row => (string)row.Values[0]).ToArray());
        Assert.Contains("SubqueryStrategy [SubqueryLoweringStrategy] _sq_2 -> PredicateSemiJoin", inspection.PlanningText);
    }

    [TestMethod]
    public void Characterization_UserCteNamedLikeGeneratedScalarMaterialization_ShouldNotCollide()
    {
        const string query = @"
            WITH _sm_1 AS (
                SELECT b.City FROM #B.entities() b
            )
            SELECT (
                SELECT b.City FROM #B.entities() b
                ORDER BY b.Population DESC
                TAKE 1
            ) AS TopCity
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateCharacterizationSources()).Run(TestContext.CancellationToken);
        var inspection = CompileSubqueryForInspection(query);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.All(row => (string)row.Values[0] == "LYON"));
        Assert.Contains("_sm_2", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void Characterization_TopLevelCteDefinitionConsumingQueryAlias_ShouldRejectWithReadableAliasMessage()
    {
        const string query = @"
            WITH bad AS (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
            )
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (
                SELECT x.City FROM bad x
            )";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateCharacterizationSources()));

        Assert.IsTrue(
            exception.Envelopes.Any(envelope => envelope.Code == DiagnosticCode.MQ3015_UnknownAlias),
            $"Expected MQ3015, got {string.Join(", ", exception.Envelopes.Select(envelope => envelope.Code))}.");
        StringAssert.Contains(exception.Message, "Unknown alias");
        StringAssert.Contains(exception.Message, "a");
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateCharacterizationSources()
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
                    new BasicEntity { City = "KRAKOW", Country = "POLAND", Population = 220m },
                    new BasicEntity { City = "GDANSK", Country = "POLAND", Population = 120m },
                    new BasicEntity { City = "MUNICH", Country = "GERMANY", Population = 250m },
                    new BasicEntity { City = "LYON", Country = "FRANCE", Population = 450m }
                ]
            },
            {
                "#C", [
                    new BasicEntity { City = "WARSAW", Country = "POLAND", Population = 220m },
                    new BasicEntity { City = "GDANSK", Country = "POLAND", Population = 50m },
                    new BasicEntity { City = "MUNICH", Country = "GERMANY", Population = 250m },
                    new BasicEntity { City = "PARIS", Country = "FRANCE", Population = 999m }
                ]
            }
        };
    }
}
