using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenInSubquery_HasEqualityCorrelation_ShouldUseLeftSemiJoin()
    {
        const string query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.City IN (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "            )"
        );

        var table = CreateAndRunVirtualMachine(query, CreateCorrelatedInSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1_corr_0", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenNotInSubquery_HasEqualityCorrelation_ShouldUseLeftAntiSemiJoin()
    {
        const string query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.City NOT IN (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "            )"
        );

        var table = CreateAndRunVirtualMachine(query, CreateCorrelatedInSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["BERLIN"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenInSubquery_HasNonEquiCorrelation_ShouldUsePartitionedRangeSemiJoin()
    {
        const string query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.City IN (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Population < a.Population\n"
            + "            )"
        );

        var table = CreateAndRunVirtualMachine(query, CreateCorrelatedInSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["BERLIN"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalSortMergeJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.Contains("[partitions:", inspection.PhysicalPlanText);
        Assert.Contains("<", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenCorrelatedInSubquery_IsInsideCteBody_ShouldCorrelateToCteBodyQuery()
    {
        const string query = (
            "\n"
            + "            WITH filtered AS (\n"
            + "                SELECT a.City FROM #A.entities() a\n"
            + "                WHERE a.City IN (\n"
            + "                    SELECT b.City FROM #B.entities() b\n"
            + "                    WHERE b.Country = a.Country\n"
            + "                )\n"
            + "            )\n"
            + "            SELECT f.City FROM filtered f"
        );

        var table = CreateAndRunVirtualMachine(query, CreateCorrelatedInSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["PARIS"]);
    }

    [TestMethod]
    public void WhenNestedInSubquery_CorrelatesToParentSubquery_ShouldDecorrelateBothLevels()
    {
        const string query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.City IN (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country IN (\n"
            + "                    SELECT c.Country FROM #C.entities() c\n"
            + "                    WHERE c.City = b.City\n"
            + "                )\n"
            + "            )"
        );

        var table = CreateAndRunVirtualMachine(query, CreateCorrelatedInSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1_corr_0", inspection.PhysicalPlanText);
        Assert.Contains("PhysicalCte", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenInSubquery_ShadowsOuterAlias_ShouldRemainUncorrelated()
    {
        const string query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.City IN (\n"
            + "                SELECT a.City FROM #B.entities() a\n"
            + "                WHERE a.Country = 'POLAND'\n"
            + "            )"
        );

        var table = CreateAndRunVirtualMachine(query, CreateCorrelatedInSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
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
                "#A",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300),
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("WARSAW", "POLAND", 100),
                    new BasicEntity("WARSAW", "GERMANY", 100),
                    new BasicEntity("BERLIN", "POLAND", 200),
                    new BasicEntity("PARIS", "FRANCE", 450),
                ]
            },
            { "#C", [new BasicEntity("WARSAW", "POLAND", 10), new BasicEntity("PARIS", "FRANCE", 20)] },
        };
    }
}
