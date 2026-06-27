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

        Assert.AreEqual(3, table.Count);
        CollectionAssert.AreEqual(
            new[] { "PARIS", "PARIS", "PARIS" },
            table.Select(row => (string)row.Values[1]).ToArray());
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

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.All(row => row.Values[0] == null));
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

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.All(row => Convert.ToDecimal(row.Values[0]) == 210m));
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

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.All(row => Convert.ToInt64(row.Values[1]) == 3L));
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

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.All(row => (string)row.Values[0] == "PARIS"));

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

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.All(row => (string)row.Values[0] == "GDANSK"));
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

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.All(row => (string)row.Values[0] == "PARIS"));

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

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.All(row => (string)row.Values[0] == "POLAND"));
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

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.All(row => Convert.ToDecimal(row.Values[0]) == 210m));
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

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("PARIS", table[0].Values[0]);
    }

}
