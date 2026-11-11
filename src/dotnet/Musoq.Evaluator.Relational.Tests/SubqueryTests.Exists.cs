using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenExistsSubquery_IsUncorrelatedAndNonEmpty_ShouldUseLeftSemiJoin()
    {
        const string query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE EXISTS (\n"
            + "                SELECT b.City, b.Country FROM #B.entities() b\n"
            + "            )"
        );

        var table = CreateAndRunVirtualMachine(query, CreateExistsSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["BERLIN"], ["PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1_key", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenExistsSubquery_IsUncorrelatedAndEmpty_ShouldReturnNoRows()
    {
        const string query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE EXISTS (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "            )"
        );

        var table = CreateAndRunVirtualMachine(query, CreateExistsSourcesWithEmptyB())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);
    }

    [TestMethod]
    public void WhenNotExistsSubquery_IsUncorrelatedAndEmpty_ShouldUseLeftAntiSemiJoin()
    {
        const string query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE NOT EXISTS (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "            )"
        );

        var table = CreateAndRunVirtualMachine(query, CreateExistsSourcesWithEmptyB())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["BERLIN"], ["PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenExistsSubquery_HasEqualityCorrelation_ShouldUseLeftSemiJoin()
    {
        const string query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE EXISTS (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "            )"
        );

        var table = CreateAndRunVirtualMachine(query, CreateExistsSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1_corr_0", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenNotExistsSubquery_HasEqualityCorrelation_ShouldUseLeftAntiSemiJoin()
    {
        const string query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE NOT EXISTS (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "            )"
        );

        var table = CreateAndRunVirtualMachine(query, CreateExistsSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["BERLIN"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftAntiSemi]", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenExistsSubquery_SelectReferencesOuterAlias_ShouldIgnoreProjectionForCorrelation()
    {
        const string query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE EXISTS (\n"
            + "                SELECT a.City FROM #B.entities() b\n"
            + "                WHERE b.Country = 'POLAND'\n"
            + "            )"
        );

        var table = CreateAndRunVirtualMachine(query, CreateExistsSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["BERLIN"], ["PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.IsFalse(inspection.PhysicalPlanText.Contains("_sq_1_corr_", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WhenExistsSubquery_IsInsideCteBody_ShouldCorrelateToCteBodyQuery()
    {
        const string query = (
            "\n"
            + "            WITH filtered AS (\n"
            + "                SELECT a.City FROM #A.entities() a\n"
            + "                WHERE EXISTS (\n"
            + "                    SELECT b.City FROM #B.entities() b\n"
            + "                    WHERE b.Country = a.Country\n"
            + "                )\n"
            + "            )\n"
            + "            SELECT f.City FROM filtered f"
        );

        var table = CreateAndRunVirtualMachine(query, CreateExistsSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["PARIS"]);
    }

    [TestMethod]
    public void WhenNestedExistsSubquery_CorrelatesToParentSubquery_ShouldDecorrelateBothLevels()
    {
        const string query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE EXISTS (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "                  AND EXISTS (\n"
            + "                      SELECT c.City FROM #C.entities() c\n"
            + "                      WHERE c.City = b.City\n"
            + "                  )\n"
            + "            )"
        );

        var table = CreateAndRunVirtualMachine(query, CreateExistsSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
        Assert.Contains("PhysicalCte", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenExistsSubquery_IsInsideOrBranch_ShouldUsePredicateHashMark()
    {
        const string query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE EXISTS (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = a.Country\n"
            + "            )\n"
            + "               OR a.Country = 'GERMANY'"
        );

        var table = CreateAndRunVirtualMachine(query, CreateExistsSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW"], ["BERLIN"], ["PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalHashJoin [LeftMark]", inspection.PhysicalPlanText);
        Assert.Contains(
            "SubqueryStrategy [SubqueryLoweringStrategy] _sq_1 -> PredicateHashMark",
            inspection.PlanningText
        );
        Assert.IsFalse(inspection.PhysicalPlanText.Contains("PhysicalHashJoin [LeftSemi]", StringComparison.Ordinal));
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateExistsSources()
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
            { "#B", [new BasicEntity("KRAKOW", "POLAND", 100), new BasicEntity("PARIS", "FRANCE", 450)] },
            { "#C", [new BasicEntity("KRAKOW", "POLAND", 10), new BasicEntity("PARIS", "FRANCE", 20)] },
        };
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateExistsSourcesWithEmptyB()
    {
        var sources = CreateExistsSources();
        sources["#B"] = Array.Empty<BasicEntity>();
        return sources;
    }
}
