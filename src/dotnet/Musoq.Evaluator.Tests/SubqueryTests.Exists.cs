using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenExistsSubquery_IsUncorrelatedAndNonEmpty_ShouldUseLeftSemiJoin()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE EXISTS (
                SELECT b.City, b.Country FROM #B.entities() b
            )";

        var table = CreateAndRunVirtualMachine(query, CreateExistsSources()).Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        CollectionAssert.AreEqual(
            new[] { "WARSAW", "BERLIN", "PARIS" },
            table.Select(row => (string)row.Values[0]).ToArray());

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1_key", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenExistsSubquery_IsUncorrelatedAndEmpty_ShouldReturnNoRows()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE EXISTS (
                SELECT b.City FROM #B.entities() b
            )";

        var table = CreateAndRunVirtualMachine(query, CreateExistsSourcesWithEmptyB()).Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenNotExistsSubquery_IsUncorrelatedAndEmpty_ShouldUseLeftAntiSemiJoin()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE NOT EXISTS (
                SELECT b.City FROM #B.entities() b
            )";

        var table = CreateAndRunVirtualMachine(query, CreateExistsSourcesWithEmptyB()).Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        CollectionAssert.AreEqual(
            new[] { "WARSAW", "BERLIN", "PARIS" },
            table.Select(row => (string)row.Values[0]).ToArray());

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenExistsSubquery_HasEqualityCorrelation_ShouldUseLeftSemiJoin()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE EXISTS (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
            )";

        var table = CreateAndRunVirtualMachine(query, CreateExistsSources()).Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        CollectionAssert.AreEqual(
            new[] { "WARSAW", "PARIS" },
            table.Select(row => (string)row.Values[0]).ToArray());

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1_corr_0", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenNotExistsSubquery_HasEqualityCorrelation_ShouldUseLeftAntiSemiJoin()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE NOT EXISTS (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
            )";

        var table = CreateAndRunVirtualMachine(query, CreateExistsSources()).Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("BERLIN", table[0].Values[0]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenExistsSubquery_SelectReferencesOuterAlias_ShouldIgnoreProjectionForCorrelation()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE EXISTS (
                SELECT a.City FROM #B.entities() b
                WHERE b.Country = 'POLAND'
            )";

        var table = CreateAndRunVirtualMachine(query, CreateExistsSources()).Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var inspection = CompileSubqueryForInspection(query);
        Assert.IsFalse(inspection.PhysicalPlanText.Contains("_sq_1_corr_", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WhenExistsSubquery_IsInsideCteBody_ShouldCorrelateToCteBodyQuery()
    {
        const string query = @"
            WITH filtered AS (
                SELECT a.City FROM #A.entities() a
                WHERE EXISTS (
                    SELECT b.City FROM #B.entities() b
                    WHERE b.Country = a.Country
                )
            )
            SELECT f.City FROM filtered f";

        var table = CreateAndRunVirtualMachine(query, CreateExistsSources()).Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        CollectionAssert.AreEqual(
            new[] { "WARSAW", "PARIS" },
            table.Select(row => (string)row.Values[0]).ToArray());
    }

    [TestMethod]
    public void WhenNestedExistsSubquery_CorrelatesToParentSubquery_ShouldDecorrelateBothLevels()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE EXISTS (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
                  AND EXISTS (
                      SELECT c.City FROM #C.entities() c
                      WHERE c.City = b.City
                  )
            )";

        var table = CreateAndRunVirtualMachine(query, CreateExistsSources()).Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        CollectionAssert.AreEqual(
            new[] { "WARSAW", "PARIS" },
            table.Select(row => (string)row.Values[0]).ToArray());

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.Contains("PhysicalCte", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenExistsSubquery_IsInsideOrBranch_ShouldKeepOuterJoinFallback()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE EXISTS (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
            )
               OR a.Country = 'GERMANY'";

        var table = CreateAndRunVirtualMachine(query, CreateExistsSources()).Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftOuter]", inspection.PhysicalPlanText);
        Assert.Contains("SubqueryStrategy [SubqueryLoweringStrategy] _sq_1 -> PredicateOuterJoinFallback", inspection.PlanningText);
        Assert.IsFalse(inspection.PhysicalPlanText.Contains("PhysicalHashJoin [LeftSemi]", StringComparison.Ordinal));
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateExistsSources()
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

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateExistsSourcesWithEmptyB()
    {
        var sources = CreateExistsSources();
        sources["#B"] = Array.Empty<BasicEntity>();
        return sources;
    }
}
