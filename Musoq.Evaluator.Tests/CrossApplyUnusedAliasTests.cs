using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests for reproducing issue where cross apply alias is defined but not used anywhere.
///     This can cause a "key not found in dictionary" exception.
/// </summary>
[TestClass]
public class CrossApplyUnusedAliasTests : GenericEntityTestBase
{
    public TestContext TestContext { get; set; }

    /// <summary>
    ///     Test case where cross apply alias 't' is defined but only columns from 'a' are selected.
    ///     The alias 't' is completely unused in the query.
    ///     Expected: Should either work gracefully or throw a clear error message.
    /// </summary>
    [TestMethod]
    public void CrossApply_WithUnusedAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = "select a.City from #schema.first() a cross apply #schema.second(a.Country) t";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "Country1", Population = 200 },
            new() { City = "City3", Country = "Country2", Population = 300 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" },
            new() { Country = "Country1", Money = 2000, Month = "February" },
            new() { Country = "Country2", Money = 3000, Month = "March" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                new ObjectRowsSource(source.Rows.Where(f => (string)f["Country"] == (string)parameters[0]).ToArray()));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
    }

    /// <summary>
    ///     Test case with two cross applies where the second alias is unused.
    /// </summary>
    [TestMethod]
    public void CrossApply_WithSecondUnusedAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = @"
            select a.City, b.Money 
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

        var thirdSource = new List<CrossApplyClass3>
        {
            new() { Id = 1, Description = "Desc1" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            thirdSource);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    /// <summary>
    ///     Test case where cross apply uses property access but the alias is not used in select.
    /// </summary>
    [TestMethod]
    public void CrossApply_PropertyAccessWithUnusedAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = "select a.City from #schema.first() a cross apply a.Values as b";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Values = [1, 2, 3] },
            new() { City = "City2", Values = [4, 5] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("a.City", table.Columns.ElementAt(0).ColumnName);
    }

    /// <summary>
    ///     Test case where cross apply alias is only used in WHERE but not in SELECT.
    /// </summary>
    [TestMethod]
    public void CrossApply_AliasUsedOnlyInWhere_ShouldWork()
    {
        const string query = @"
            select a.City 
            from #schema.first() a 
            cross apply #schema.second(a.Country) t 
            where t.Money > 1500";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "Country2", Population = 200 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" },
            new() { Country = "Country1", Money = 2000, Month = "February" },
            new() { Country = "Country2", Money = 3000, Month = "March" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                new ObjectRowsSource(source.Rows.Where(f => (string)f["Country"] == (string)parameters[0]).ToArray()));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    /// <summary>
    ///     Test case where the first alias 'a' is not used anywhere (but cross apply's 'b' is used).
    /// </summary>
    [TestMethod]
    public void CrossApply_FirstAliasUnused_ShouldNotThrowKeyNotFound()
    {
        const string query = "select b.Money from #schema.first() a cross apply #schema.second(a.Country) b";

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
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                new ObjectRowsSource(source.Rows.Where(f => (string)f["Country"] == (string)parameters[0]).ToArray()));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("b.Money", table.Columns.ElementAt(0).ColumnName);
    }

    /// <summary>
    ///     Test case with only literal values selected (no alias used at all).
    /// </summary>
    [TestMethod]
    public void CrossApply_NoAliasUsedInSelect_ShouldNotThrowKeyNotFound()
    {
        const string query = "select 1 as Value from #schema.first() a cross apply #schema.second(a.Country) t";

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
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                new ObjectRowsSource(source.Rows.Where(f => (string)f["Country"] == (string)parameters[0]).ToArray()));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    /// <summary>
    ///     Test case where cross apply with CTE and unused alias.
    /// </summary>
    [TestMethod]
    public void CrossApply_CteWithUnusedAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = @"
            with cte as (
                select a.City as City, a.Country as Country 
                from #schema.first() a
            )
            select c.City from cte c cross apply #schema.second(c.Country) t";

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
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                new ObjectRowsSource(source.Rows.Where(f => (string)f["Country"] == (string)parameters[0]).ToArray()));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    /// <summary>
    ///     Test case where cross apply with method call on alias but alias not used in select.
    /// </summary>
    [TestMethod]
    public void CrossApply_MethodCallWithUnusedAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = @"
            select a.City
            from #schema.first() a
            cross apply a.Split(a.City, ',') t2";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1,City2", Country = "Country1", Population = 100 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    /// <summary>
    ///     Test case CTE cross apply with method call and unused alias.
    /// </summary>
    [TestMethod]
    public void CrossApply_CteWithMethodCallAndUnusedAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = @"
            with testX as (
                select 'hello world' as Text
                from #schema.first() a
            )
            select t.Text
            from testX t
            cross apply t.Split(t.Text, ' ') unused";

        var firstSource = new object[1] { new { } };
        var vm = CreateAndRunVirtualMachine(query, firstSource);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    /// <summary>
    ///     Test star expansion with cross apply where second alias is not explicitly used.
    /// </summary>
    [TestMethod]
    public void CrossApply_StarExpansionWithUnusedSecondAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = "select a.* from #schema.first() a cross apply #schema.second(a.Country) t";

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
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                new ObjectRowsSource(source.Rows.Where(f => (string)f["Country"] == (string)parameters[0]).ToArray()));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    /// <summary>
    ///     Test where cross apply is chained but middle alias is unused.
    /// </summary>
    [TestMethod]
    public void CrossApply_ChainedWithMiddleAliasUnused_ShouldNotThrowKeyNotFound()
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

        Assert.IsNotNull(table);
    }

    /// <summary>
    ///     Test with multiple chained cross applies where intermediate aliases are unused.
    /// </summary>
    [TestMethod]
    public void CrossApply_MultipleChained_WithIntermediateUnusedAliases_ShouldNotThrowKeyNotFound()
    {
        const string query = @"
            select a.City
            from #schema.first() a
            cross apply #schema.second(a.Country) b
            cross apply #schema.second(a.Country) c";

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
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                new ObjectRowsSource(source.Rows.Where(f => (string)f["Country"] == (string)parameters[0]).ToArray()));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    /// <summary>
    ///     Test outer apply with unused alias.
    /// </summary>
    [TestMethod]
    public void OuterApply_WithUnusedAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = "select a.City from #schema.first() a outer apply #schema.second(a.Country) t";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "NoMatch", Population = 200 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                new ObjectRowsSource(source.Rows.Where(f => (string)f["Country"] == (string)parameters[0]).ToArray()));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    /// <summary>
    ///     Test cross apply with count(*) aggregation where cross apply alias is not directly used.
    /// </summary>
    [TestMethod]
    public void CrossApply_WithCountAggregation_UnusedAliasInSelect_ShouldNotThrowKeyNotFound()
    {
        const string query = @"
            select a.City, a.Count(1) as Cnt
            from #schema.first() a
            cross apply #schema.second(a.Country) t
            group by a.City";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" },
            new() { Country = "Country1", Money = 2000, Month = "February" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                new ObjectRowsSource(source.Rows.Where(f => (string)f["Country"] == (string)parameters[0]).ToArray()));

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, (int)table[0].Values[1]);
    }

    /// <summary>
    ///     Test case where cross apply with only accessing properties through nested path.
    /// </summary>
    [TestMethod]
    public void CrossApply_NestedPropertyAccess_UnusedIntermediateAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = "select a.City from #schema.first() a cross apply a.Values as b";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100, Values = [1, 2, 3] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
    }

    /// <summary>
    ///     Test cross apply followed by a join where the cross apply alias is unused.
    /// </summary>
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

    /// <summary>
    ///     Test CTE with cross apply followed by join.
    /// </summary>
    [TestMethod]
    public void Cte_CrossApply_FollowedByJoin_ShouldWork()
    {
        const string query = @"
            with cte as (
                select a.City as City, a.Country as Country
                from #schema.first() a
            )
            select c.City, t.Money, j.Month
            from cte c
            cross apply #schema.second(c.Country) t
            inner join #schema.third() j on c.Country = j.Country";

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

    /// <summary>
    ///     Test CTE with cross apply followed by left outer join.
    /// </summary>
    [TestMethod]
    public void Cte_CrossApply_FollowedByLeftOuterJoin_ShouldWork()
    {
        const string query = @"
            with cte as (
                select a.City as City, a.Country as Country
                from #schema.first() a
            )
            select c.City, t.Money, j.Month
            from cte c
            cross apply #schema.second(c.Country) t
            left outer join #schema.third() j on c.Country = j.Country";

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

        Assert.IsNotNull(table);
        Assert.AreEqual(4, table.Columns.Count());
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

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Columns.Count());
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

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Columns.Count());
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

        Assert.IsNotNull(table);
        Assert.AreEqual(4, table.Columns.Count());
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

        Assert.IsNotNull(table);
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

        Assert.IsNotNull(table);
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

        Assert.IsNotNull(table);
    }

    public class CrossApplyClass1
    {
        public string City { get; set; }
        public string Country { get; set; }
        public int Population { get; set; }
        public int[] Values { get; set; }
    }

    public class CrossApplyClass2
    {
        public string Country { get; set; }
        public decimal Money { get; set; }
        public string Month { get; set; }
    }

    public class CrossApplyClass3
    {
        public int Id { get; set; }
        public string Description { get; set; }
    }

    public class CrossApplyMultiProperty
    {
        public string Name { get; set; }
        public int[] Values1 { get; set; }
        public int[] Values2 { get; set; }
        public int[] Values3 { get; set; }
    }

    public class CrossApplyNestedProperty
    {
        public string Name { get; set; }
        public NestedValue[] NestedValues { get; set; }
    }

    public class NestedValue
    {
        public int Value { get; set; }
    }

    #region Multiple Property Cross Applies - Dictionary Key Issue

    /// <summary>
    ///     Test multiple cross applies on different properties from same source.
    ///     Pattern: m.A a cross apply m.B b - where both A and B are properties of m.
    /// </summary>
    [TestMethod]
    public void CrossApply_TwoPropertiesFromSameSource_ShouldWork()
    {
        const string query = @"
            select 1
            from #schema.first() m
            cross apply m.Values1 a
            cross apply m.Values2 b";

        var firstSource = new List<CrossApplyMultiProperty>
        {
            new() { Name = "Test1", Values1 = [1, 2], Values2 = [10, 20] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    /// <summary>
    ///     Test multiple cross applies on properties - selecting from applied aliases.
    /// </summary>
    [TestMethod]
    public void CrossApply_TwoPropertiesFromSameSource_SelectFromBoth_ShouldWork()
    {
        const string query = @"
            select a.Value, b.Value
            from #schema.first() m
            cross apply m.Values1 a
            cross apply m.Values2 b";

        var firstSource = new List<CrossApplyMultiProperty>
        {
            new() { Name = "Test1", Values1 = [1, 2], Values2 = [10, 20] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Columns.Count());
    }

    /// <summary>
    ///     Test multiple cross applies on properties - selecting only from second applied alias.
    /// </summary>
    [TestMethod]
    public void CrossApply_TwoPropertiesFromSameSource_SelectOnlyFromSecond_ShouldWork()
    {
        const string query = @"
            select b.Value
            from #schema.first() m
            cross apply m.Values1 a
            cross apply m.Values2 b";

        var firstSource = new List<CrossApplyMultiProperty>
        {
            new() { Name = "Test1", Values1 = [1, 2], Values2 = [10, 20] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    /// <summary>
    ///     Test multiple cross applies on properties - original source alias used in select.
    /// </summary>
    [TestMethod]
    public void CrossApply_TwoPropertiesFromSameSource_SelectFromOriginal_ShouldWork()
    {
        const string query = @"
            select m.Name, a.Value, b.Value
            from #schema.first() m
            cross apply m.Values1 a
            cross apply m.Values2 b";

        var firstSource = new List<CrossApplyMultiProperty>
        {
            new() { Name = "Test1", Values1 = [1, 2], Values2 = [10, 20] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Columns.Count());
    }

    /// <summary>
    ///     Test three cross applies on properties from same source.
    /// </summary>
    [TestMethod]
    public void CrossApply_ThreePropertiesFromSameSource_ShouldWork()
    {
        const string query = @"
            select a.Value, b.Value, c.Value
            from #schema.first() m
            cross apply m.Values1 a
            cross apply m.Values2 b
            cross apply m.Values3 c";

        var firstSource = new List<CrossApplyMultiProperty>
        {
            new() { Name = "Test1", Values1 = [1], Values2 = [10], Values3 = [100] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Columns.Count());
    }

    /// <summary>
    ///     Test CTE with multiple property cross applies from same source.
    /// </summary>
    [TestMethod]
    public void Cte_CrossApply_TwoPropertiesFromSameSource_ShouldWork()
    {
        const string query = @"
            with cte as (
                select m.Name as Name, m.Values1 as Values1, m.Values2 as Values2
                from #schema.first() m
            )
            select c.Name, a.Value, b.Value
            from cte c
            cross apply c.Values1 a
            cross apply c.Values2 b";

        var firstSource = new List<CrossApplyMultiProperty>
        {
            new() { Name = "Test1", Values1 = [1, 2], Values2 = [10, 20] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    /// <summary>
    ///     Test cross apply on property, then method call on same source.
    /// </summary>
    [TestMethod]
    public void CrossApply_PropertyThenMethodOnSameSource_ShouldWork()
    {
        const string query = @"
            select a.Value, b.Value
            from #schema.first() m
            cross apply m.Values1 a
            cross apply m.Split(m.Name, ',') b";

        var firstSource = new List<CrossApplyMultiProperty>
        {
            new() { Name = "A,B,C", Values1 = [1, 2], Values2 = [10, 20], Values3 = [100] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    /// <summary>
    ///     Test cross apply chain: source -> property -> property on applied result.
    /// </summary>
    [TestMethod]
    public void CrossApply_ChainedPropertyAccess_ShouldWork()
    {
        const string query = @"
            select m.Name, a.Value
            from #schema.first() m
            cross apply m.NestedValues a";

        var firstSource = new List<CrossApplyNestedProperty>
        {
            new() { Name = "Test1", NestedValues = [new NestedValue { Value = 1 }, new NestedValue { Value = 2 }] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Columns.Count());
    }

    /// <summary>
    ///     Test outer apply on two properties from same source.
    /// </summary>
    [TestMethod]
    public void OuterApply_TwoPropertiesFromSameSource_ShouldWork()
    {
        const string query = @"
            select m.Name, a.Value, b.Value
            from #schema.first() m
            outer apply m.Values1 a
            outer apply m.Values2 b";

        var firstSource = new List<CrossApplyMultiProperty>
        {
            new() { Name = "Test1", Values1 = [1, 2], Values2 = [10, 20] },
            new() { Name = "Test2", Values1 = [], Values2 = [30] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    /// <summary>
    ///     Test mixed cross apply and outer apply on properties from same source.
    /// </summary>
    [TestMethod]
    public void CrossApply_ThenOuterApply_OnPropertiesFromSameSource_ShouldWork()
    {
        const string query = @"
            select m.Name, a.Value, b.Value
            from #schema.first() m
            cross apply m.Values1 a
            outer apply m.Values2 b";

        var firstSource = new List<CrossApplyMultiProperty>
        {
            new() { Name = "Test1", Values1 = [1, 2], Values2 = [] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    #endregion
}
