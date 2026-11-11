using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenScalarSubquery_IsInSelect_ShouldReturnSingleValueForEachOuterRow()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = 'FRANCE'\n"
            + "            ) AS MatchCity\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("MatchCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "PARIS"],
            ["BERLIN", "PARIS"],
            ["PARIS", "PARIS"]
        );
    }

    [TestMethod]
    public void WhenScalarSubquery_ReturnsNoRows_ShouldReturnNull()
    {
        const string query = (
            "\n"
            + "            SELECT (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = 'SPAIN'\n"
            + "            ) AS MissingCity\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("MissingCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            new object?[] { null },
            new object?[] { null },
            new object?[] { null }
        );
    }

    [TestMethod]
    public void WhenScalarSubquery_ReturnsMultipleRows_ShouldThrow()
    {
        const string query = (
            "\n"
            + "            SELECT (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = 'POLAND'\n"
            + "            ) AS City\n"
            + "            FROM #A.entities() a"
        );

        var vm = CreateAndRunVirtualMachine(query, CreateScalarSources());

        Assert.Throws<InvalidOperationException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasUncorrelatedAggregate_ShouldReturnAggregateValue()
    {
        const string query = (
            "\n"
            + "            SELECT (\n"
            + "                SELECT Sum(b.Population) FROM #B.entities() b\n"
            + "                WHERE b.Country = 'POLAND'\n"
            + "            ) AS TotalPopulation\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("TotalPopulation", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [210m], [210m], [210m]);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasUncorrelatedCount_ShouldReturnLongValue()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT Count(1) FROM #B.entities() b\n"
            + "            ) AS Total\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Total", typeof(long?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW", 3L], ["BERLIN", 3L], ["PARIS", 3L]);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasUncorrelatedCustomAggregate_ShouldReturnAggregateValue()
    {
        const string query = (
            "\n"
            + "            SELECT a.City, (\n"
            + "                SELECT CustomRowCount() FROM #B.entities() b\n"
            + "            ) AS Total\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Total", typeof(long?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW", 3L], ["BERLIN", 3L], ["PARIS", 3L]);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasScalarFunctionProjectionWithMultipleRows_ShouldThrow()
    {
        const string query = (
            "\n"
            + "            SELECT (\n"
            + "                SELECT DoNothing(b.City) FROM #B.entities() b\n"
            + "                WHERE b.Country = 'POLAND'\n"
            + "            ) AS City\n"
            + "            FROM #A.entities() a"
        );

        var vm = CreateAndRunVirtualMachine(query, CreateScalarSources());

        Assert.Throws<InvalidOperationException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasOrderByAndTake_ShouldMaterializeBeforeCardinalityCheck()
    {
        const string query = (
            "\n"
            + "            SELECT (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                ORDER BY b.Population DESC\n"
            + "                TAKE 1\n"
            + "            ) AS TopCity\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("TopCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["PARIS"], ["PARIS"], ["PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("_sm_1", inspection.PhysicalPlanText);
        Assert.Contains("_sq_1_value", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasOrderBySkipAndTake_ShouldReturnSlicedRow()
    {
        const string query = (
            "\n"
            + "            SELECT (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                ORDER BY b.Population\n"
            + "                SKIP 1\n"
            + "                TAKE 1\n"
            + "            ) AS SecondCity\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("SecondCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["GDANSK"], ["GDANSK"], ["GDANSK"]);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasUnionSingleValue_ShouldMaterializeSetResult()
    {
        const string query = (
            "\n"
            + "            SELECT (\n"
            + "                SELECT b.City FROM #B.entities() b WHERE b.City = 'PARIS'\n"
            + "                UNION (City)\n"
            + "                SELECT c.City FROM #C.entities() c WHERE c.City = 'PARIS'\n"
            + "            ) AS City\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["PARIS"], ["PARIS"], ["PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("_sm_1", inspection.PhysicalPlanText);
        Assert.Contains("ScalarLeftJoin", inspection.PlanningText);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasUnionMultipleValues_ShouldPreserveScalarCardinalityCheck()
    {
        const string query = (
            "\n"
            + "            SELECT (\n"
            + "                SELECT b.City FROM #B.entities() b WHERE b.Country = 'POLAND'\n"
            + "                UNION (City)\n"
            + "                SELECT c.City FROM #C.entities() c WHERE c.Country = 'FRANCE'\n"
            + "            ) AS City\n"
            + "            FROM #A.entities() a"
        );

        var vm = CreateAndRunVirtualMachine(query, CreateScalarSources());

        Assert.Throws<InvalidOperationException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasDistinctSingleValue_ShouldReturnValue()
    {
        const string query = (
            "\n"
            + "            SELECT (\n"
            + "                SELECT DISTINCT b.Country FROM #B.entities() b\n"
            + "                WHERE b.Country = 'POLAND'\n"
            + "            ) AS Country\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["POLAND"], ["POLAND"], ["POLAND"]);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasDistinctMultipleValues_ShouldThrow()
    {
        const string query = (
            "\n"
            + "            SELECT (\n"
            + "                SELECT DISTINCT b.Country FROM #B.entities() b\n"
            + "            ) AS Country\n"
            + "            FROM #A.entities() a"
        );

        var vm = CreateAndRunVirtualMachine(query, CreateScalarSources());

        Assert.Throws<InvalidOperationException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasGroupBySingleGroup_ShouldReturnAggregateValue()
    {
        const string query = (
            "\n"
            + "            SELECT (\n"
            + "                SELECT Sum(b.Population) FROM #B.entities() b\n"
            + "                WHERE b.Country = 'POLAND'\n"
            + "                GROUP BY b.Country\n"
            + "            ) AS TotalPopulation\n"
            + "            FROM #A.entities() a"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("TotalPopulation", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [210m], [210m], [210m]);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasGroupByMultipleGroups_ShouldThrow()
    {
        const string query = (
            "\n"
            + "            SELECT (\n"
            + "                SELECT Sum(b.Population) FROM #B.entities() b\n"
            + "                GROUP BY b.Country\n"
            + "            ) AS TotalPopulation\n"
            + "            FROM #A.entities() a"
        );

        var vm = CreateAndRunVirtualMachine(query, CreateScalarSources());

        Assert.Throws<InvalidOperationException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
    }

    [TestMethod]
    public void WhenScalarSubquery_IsInWhere_ShouldFilterBySingleValue()
    {
        const string query = (
            "\n"
            + "            SELECT a.City FROM #A.entities() a\n"
            + "            WHERE a.City = (\n"
            + "                SELECT b.City FROM #B.entities() b\n"
            + "                WHERE b.Country = 'FRANCE'\n"
            + "            )"
        );

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["PARIS"]);
    }
}
