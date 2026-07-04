using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenScalarSubquery_IsInSelect_ShouldReturnSingleValueForEachOuterRow()
    {
        const string query = @"
            SELECT a.City, (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = 'FRANCE'
            ) AS MatchCity
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("MatchCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "PARIS"],
            ["BERLIN", "PARIS"],
            ["PARIS", "PARIS"]);
    }

    [TestMethod]
    public void WhenScalarSubquery_ReturnsNoRows_ShouldReturnNull()
    {
        const string query = @"
            SELECT (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = 'SPAIN'
            ) AS MissingCity
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("MissingCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            new object?[] { null },
            new object?[] { null },
            new object?[] { null });
    }

    [TestMethod]
    public void WhenScalarSubquery_ReturnsMultipleRows_ShouldThrow()
    {
        const string query = @"
            SELECT (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = 'POLAND'
            ) AS City
            FROM #A.entities() a";

        var vm = CreateAndRunVirtualMachine(query, CreateScalarSources());

        Assert.Throws<InvalidOperationException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasUncorrelatedAggregate_ShouldReturnAggregateValue()
    {
        const string query = @"
            SELECT (
                SELECT Sum(b.Population) FROM #B.entities() b
                WHERE b.Country = 'POLAND'
            ) AS TotalPopulation
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("TotalPopulation", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [210m], [210m], [210m]);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasUncorrelatedCount_ShouldReturnLongValue()
    {
        const string query = @"
            SELECT a.City, (
                SELECT Count(1) FROM #B.entities() b
            ) AS Total
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("Total", typeof(long?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", 3L],
            ["BERLIN", 3L],
            ["PARIS", 3L]);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasUncorrelatedCustomAggregate_ShouldReturnAggregateValue()
    {
        const string query = @"
            SELECT a.City, (
                SELECT CustomRowCount() FROM #B.entities() b
            ) AS Total
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("Total", typeof(long?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", 3L],
            ["BERLIN", 3L],
            ["PARIS", 3L]);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasScalarFunctionProjectionWithMultipleRows_ShouldThrow()
    {
        const string query = @"
            SELECT (
                SELECT DoNothing(b.City) FROM #B.entities() b
                WHERE b.Country = 'POLAND'
            ) AS City
            FROM #A.entities() a";

        var vm = CreateAndRunVirtualMachine(query, CreateScalarSources());

        Assert.Throws<InvalidOperationException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasOrderByAndTake_ShouldMaterializeBeforeCardinalityCheck()
    {
        const string query = @"
            SELECT (
                SELECT b.City FROM #B.entities() b
                ORDER BY b.Population DESC
                TAKE 1
            ) AS TopCity
            FROM #A.entities() a";

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
        const string query = @"
            SELECT (
                SELECT b.City FROM #B.entities() b
                ORDER BY b.Population
                SKIP 1
                TAKE 1
            ) AS SecondCity
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("SecondCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["GDANSK"], ["GDANSK"], ["GDANSK"]);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasUnionSingleValue_ShouldMaterializeSetResult()
    {
        const string query = @"
            SELECT (
                SELECT b.City FROM #B.entities() b WHERE b.City = 'PARIS'
                UNION (City)
                SELECT c.City FROM #C.entities() c WHERE c.City = 'PARIS'
            ) AS City
            FROM #A.entities() a";

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
        const string query = @"
            SELECT (
                SELECT b.City FROM #B.entities() b WHERE b.Country = 'POLAND'
                UNION (City)
                SELECT c.City FROM #C.entities() c WHERE c.Country = 'FRANCE'
            ) AS City
            FROM #A.entities() a";

        var vm = CreateAndRunVirtualMachine(query, CreateScalarSources());

        Assert.Throws<InvalidOperationException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasDistinctSingleValue_ShouldReturnValue()
    {
        const string query = @"
            SELECT (
                SELECT DISTINCT b.Country FROM #B.entities() b
                WHERE b.Country = 'POLAND'
            ) AS Country
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["POLAND"], ["POLAND"], ["POLAND"]);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasDistinctMultipleValues_ShouldThrow()
    {
        const string query = @"
            SELECT (
                SELECT DISTINCT b.Country FROM #B.entities() b
            ) AS Country
            FROM #A.entities() a";

        var vm = CreateAndRunVirtualMachine(query, CreateScalarSources());

        Assert.Throws<InvalidOperationException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasGroupBySingleGroup_ShouldReturnAggregateValue()
    {
        const string query = @"
            SELECT (
                SELECT Sum(b.Population) FROM #B.entities() b
                WHERE b.Country = 'POLAND'
                GROUP BY b.Country
            ) AS TotalPopulation
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("TotalPopulation", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [210m], [210m], [210m]);
    }

    [TestMethod]
    public void WhenScalarSubquery_HasGroupByMultipleGroups_ShouldThrow()
    {
        const string query = @"
            SELECT (
                SELECT Sum(b.Population) FROM #B.entities() b
                GROUP BY b.Country
            ) AS TotalPopulation
            FROM #A.entities() a";

        var vm = CreateAndRunVirtualMachine(query, CreateScalarSources());

        Assert.Throws<InvalidOperationException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
    }

    [TestMethod]
    public void WhenScalarSubquery_IsInWhere_ShouldFilterBySingleValue()
    {
        const string query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City = (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = 'FRANCE'
            )";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["PARIS"]);
    }

}
