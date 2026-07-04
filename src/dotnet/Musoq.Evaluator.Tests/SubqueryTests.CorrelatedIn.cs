using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenInSubquery_HasEqualityCorrelation_ShouldUseLeftSemiJoin()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
            )";

        var table = CreateAndRunVirtualMachine(query, CreateCorrelatedInSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1_corr_0", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenNotInSubquery_HasEqualityCorrelation_ShouldUseLeftAntiSemiJoin()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City NOT IN (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
            )";

        var table = CreateAndRunVirtualMachine(query, CreateCorrelatedInSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["BERLIN"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenInSubquery_HasNonEquiCorrelation_ShouldKeepResidualPredicate()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (
                SELECT b.City FROM #B.entities() b
                WHERE b.Population < a.Population
            )";

        var table = CreateAndRunVirtualMachine(query, CreateCorrelatedInSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["BERLIN"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.Contains("<", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenCorrelatedInSubquery_IsInsideCteBody_ShouldCorrelateToCteBodyQuery()
    {
        const string query = @"
            WITH filtered AS (
                SELECT a.City FROM #A.entities() a
                WHERE a.City IN (
                    SELECT b.City FROM #B.entities() b
                    WHERE b.Country = a.Country
                )
            )
            SELECT f.City FROM filtered f";

        var table = CreateAndRunVirtualMachine(query, CreateCorrelatedInSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("f.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["PARIS"]);
    }

    [TestMethod]
    public void WhenNestedInSubquery_CorrelatesToParentSubquery_ShouldDecorrelateBothLevels()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country IN (
                    SELECT c.Country FROM #C.entities() c
                    WHERE c.City = b.City
                )
            )";

        var table = CreateAndRunVirtualMachine(query, CreateCorrelatedInSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1_corr_0", inspection.PhysicalPlanText);
        Assert.Contains("PhysicalCte", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenInSubquery_ShadowsOuterAlias_ShouldRemainUncorrelated()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (
                SELECT a.City FROM #B.entities() a
                WHERE a.Country = 'POLAND'
            )";

        var table = CreateAndRunVirtualMachine(query, CreateCorrelatedInSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["BERLIN"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.IsFalse(inspection.PhysicalPlanText.Contains("_sq_1_corr_", StringComparison.Ordinal));
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateCorrelatedInSources()
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
                    new BasicEntity("WARSAW", "POLAND", 100),
                    new BasicEntity("WARSAW", "GERMANY", 100),
                    new BasicEntity("BERLIN", "POLAND", 200),
                    new BasicEntity("PARIS", "FRANCE", 450)
                ]
            },
            {
                "#C", [
                    new BasicEntity("WARSAW", "POLAND", 10),
                    new BasicEntity("PARIS", "FRANCE", 20)
                ]
            }
        };
    }
}
