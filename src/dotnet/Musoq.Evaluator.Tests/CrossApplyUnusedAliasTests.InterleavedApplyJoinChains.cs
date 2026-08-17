using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class CrossApplyUnusedAliasTests
{
    /// <summary>
    ///     Test multiple cross applies followed by join.
    /// </summary>
    [TestMethod]
    public void MultipleCrossApplies_FollowedByJoin_ShouldWork()
    {
        const string query = @"
            select a.City, t1.Money, t2.Money as Money2, j.Month
            from #schema.first() a
            cross apply #schema.second(a.Country) t1
            cross apply #schema.second(a.Country) t2
            inner join #schema.third() j on a.Country = j.Country";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" }
        }.ToArray();

        var thirdSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 5000, Month = "March" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            thirdSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("t1.Money", typeof(decimal)),
            ("Money2", typeof(decimal)),
            ("j.Month", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["City1", 1000m, 1000m, "March"]);
    }

    /// <summary>
    ///     Test join followed by cross apply (reverse order).
    /// </summary>
    [TestMethod]
    public void Join_FollowedByCrossApply_ShouldWork()
    {
        const string query = @"
            select a.City, j.Month, t.Money
            from #schema.first() a
            inner join #schema.third() j on a.Country = j.Country
            cross apply #schema.second(a.Country) t";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" }
        }.ToArray();

        var thirdSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 5000, Month = "March" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            thirdSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("j.Month", typeof(string)),
            ("t.Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["City1", "March", 1000m]);
    }

    /// <summary>
    ///     Test CTE with join followed by cross apply.
    /// </summary>
    [TestMethod]
    public void Cte_Join_FollowedByCrossApply_ShouldWork()
    {
        const string query = @"
            with cte as (
                select a.City as City, a.Country as Country
                from #schema.first() a
            )
            select c.City, j.Month, t.Money
            from cte c
            inner join #schema.third() j on c.Country = j.Country
            cross apply #schema.second(c.Country) t";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" }
        }.ToArray();

        var thirdSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 5000, Month = "March" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            thirdSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("c.City", typeof(string)),
            ("j.Month", typeof(string)),
            ("t.Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["City1", "March", 1000m]);
    }

    /// <summary>
    ///     Test cross apply, join, cross apply interleaved.
    /// </summary>
    [TestMethod]
    public void CrossApply_Join_CrossApply_Interleaved_ShouldWork()
    {
        const string query = @"
            select a.City, t1.Money, j.Month, t2.Money as Money2
            from #schema.first() a
            cross apply #schema.second(a.Country) t1
            inner join #schema.third() j on a.Country = j.Country
            cross apply #schema.second(a.Country) t2";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" }
        }.ToArray();

        var thirdSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 5000, Month = "March" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            thirdSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("t1.Money", typeof(decimal)),
            ("j.Month", typeof(string)),
            ("Money2", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["City1", 1000m, "March", 1000m]);
    }

    /// <summary>
    ///     Test multiple cross applies where only first and last aliases are used.
    /// </summary>
    [TestMethod]
    public void CrossApply_MultipleWithOnlyFirstAndLastUsed_ShouldNotThrowKeyNotFound()
    {
        const string query = @"
            select a.City, c.Money
            from #schema.first() a
            cross apply #schema.second(a.Country) b
            cross apply #schema.third() c";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" }
        }.ToArray();

        var thirdSource = new List<CrossApplyClass2>
        {
            new() { Country = "Any", Money = 5000, Month = "March" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            thirdSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("c.Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["City1", 5000m]);
    }

    /// <summary>
    ///     Test cross apply with a standalone function call (Test() t pattern).
    ///     This is the exact pattern from the reported issue.
    /// </summary>
    [TestMethod]
    public void CrossApply_StandaloneFunction_WithUnusedAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = "select a.City from #schema.first() a cross apply #schema.second() t";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["City1"]);
    }

    /// <summary>
    ///     Test cross apply with standalone function call without any parameters.
    /// </summary>
    [TestMethod]
    public void CrossApply_NoParamsFunction_UnusedAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = "select a.City from #schema.first() a cross apply #schema.third() t";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" }
        }.ToArray();

        var thirdSource = new List<CrossApplyClass3>
        {
            new() { Id = 1, Description = "Desc" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            thirdSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["City1"]);
    }
}
