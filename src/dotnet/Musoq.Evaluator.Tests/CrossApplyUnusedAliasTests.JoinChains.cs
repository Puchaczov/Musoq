using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class CrossApplyUnusedAliasTests
{
    [TestMethod]
    public void CrossApply_FollowedByJoin_WithUnusedMiddleAlias_ShouldWork()
    {
        const string query = @"
            select a.City, c.Country
            from #schema.first() a
            cross apply #schema.second(a.Country) t
            inner join #schema.third() c on a.Country = c.Country";

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

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("c.Country", table.Columns.ElementAt(1).ColumnName);
    }

    /// <summary>
    ///     Test cross apply followed by join where all aliases are used.
    /// </summary>
    [TestMethod]
    public void CrossApply_FollowedByJoin_AllAliasesUsed_ShouldWork()
    {
        const string query = @"
            select a.City, t.Money, c.Month
            from #schema.first() a
            cross apply #schema.second(a.Country) t
            inner join #schema.third() c on a.Country = c.Country";

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

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("t.Money", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual("c.Month", table.Columns.ElementAt(2).ColumnName);
    }

    /// <summary>
    ///     Test cross apply followed by left outer join.
    /// </summary>
    [TestMethod]
    public void CrossApply_FollowedByLeftOuterJoin_ShouldWork()
    {
        const string query = @"
            select a.City, t.Money, c.Month
            from #schema.first() a
            cross apply #schema.second(a.Country) t
            left outer join #schema.third() c on a.Country = c.Country";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "NoMatch", Population = 200 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" },
            new() { Country = "NoMatch", Money = 2000, Month = "February" }
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

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Columns.Count());
    }

    /// <summary>
    ///     Test cross apply followed by right outer join.
    /// </summary>
    [TestMethod]
    public void CrossApply_FollowedByRightOuterJoin_ShouldWork()
    {
        const string query = @"
            select a.City, t.Money, c.Month
            from #schema.first() a
            cross apply #schema.second(a.Country) t
            right outer join #schema.third() c on a.Country = c.Country";

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
            new() { Country = "Country1", Money = 5000, Month = "March" },
            new() { Country = "NoMatch", Money = 6000, Month = "April" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            thirdSource);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Columns.Count());
    }

    /// <summary>
    ///     Test outer apply followed by inner join.
    /// </summary>
    [TestMethod]
    public void OuterApply_FollowedByInnerJoin_ShouldWork()
    {
        const string query = @"
            select a.City, t.Money, c.Month
            from #schema.first() a
            outer apply #schema.second(a.Country) t
            inner join #schema.third() c on a.Country = c.Country";

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

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Columns.Count());
    }

}
