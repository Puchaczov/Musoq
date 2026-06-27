using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryCrossApplyBasicsTests
{
    #region Exploration 5: Cross Apply with Group By

    [TestMethod]
    public void Explore5_CrossApply_WithGroupByOnSource_ShouldWork()
    {
        const string query = @"
            select p.Name, Count(t.Value) as TagCount
            from #schema.first() p
            cross apply p.Tags t
            group by p.Name";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b", "c"] },
            new() { Name = "Jane", Age = 25, Tags = ["x", "y"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Explore5_CrossApply_WithGroupByOnAppliedValue_ShouldWork()
    {
        const string query = @"
            select t.Value, Count(p.Name) as PersonCount
            from #schema.first() p
            cross apply p.Tags t
            group by t.Value";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["common", "unique1"] },
            new() { Name = "Jane", Age = 25, Tags = ["common", "unique2"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    [TestMethod]
    public void Explore5_CrossApply_WithGroupByHaving_ShouldWork()
    {
        const string query = @"
            select p.Name, Sum(s.Value) as TotalScore
            from #schema.first() p
            cross apply p.Scores s
            group by p.Name
            having Sum(s.Value) > 10";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [1, 2, 3] },
            new() { Name = "Jane", Age = 25, Scores = [10, 20, 30] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Jane", table[0].Values[0]);
    }

    #endregion

    #region Exploration 6: Cross Apply with Join

    [TestMethod]
    public void Explore6_CrossApply_ThenInnerJoin_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value, o.OrderId
            from #schema.first() p
            cross apply p.Tags t
            inner join #schema.second() o on p.Name = o.CustomerName";

        var persons = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["vip"] }
        }.ToArray();

        var orders = new List<Order>
        {
            new() { OrderId = 1, CustomerName = "John", Total = 100 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons, orders);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    [TestMethod]
    public void Explore6_InnerJoin_ThenCrossApply_ShouldWork()
    {
        const string query = @"
            select p.Name, o.OrderId, t.Value
            from #schema.first() p
            inner join #schema.second() o on p.Name = o.CustomerName
            cross apply p.Tags t";

        var persons = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["vip", "premium"] }
        }.ToArray();

        var orders = new List<Order>
        {
            new() { OrderId = 1, CustomerName = "John", Total = 100 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons, orders);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    [TestMethod]
    public void Explore6_CrossApply_ThenLeftJoin_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value, o.OrderId
            from #schema.first() p
            cross apply p.Tags t
            left outer join #schema.second() o on p.Name = o.CustomerName";

        var persons = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["vip"] },
            new() { Name = "Jane", Age = 25, Tags = ["new"] }
        }.ToArray();

        var orders = new List<Order>
        {
            new() { OrderId = 1, CustomerName = "John", Total = 100 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons, orders);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    #endregion

    #region Exploration 7: Outer Apply Edge Cases

    [TestMethod]
    public void Explore7_OuterApply_WithEmptyArray_ShouldReturnNull()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            outer apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = [] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void Explore7_OuterApply_WithNullArray_ShouldReturnNull()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            outer apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = null }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    [TestMethod]
    public void Explore7_MixedCrossAndOuterApply_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value, s.Value
            from #schema.first() p
            cross apply p.Tags t
            outer apply p.Scores s";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a"], Scores = [] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    #endregion

    #region Exploration 14: Aggregation edge cases

    [TestMethod]
    public void Explore14_CrossApply_WithMultipleAggregates_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                Count(s.Value) as ScoreCount,
                Sum(s.Value) as TotalScore,
                Avg(s.Value) as AvgScore,
                Min(s.Value) as MinScore,
                Max(s.Value) as MaxScore
            from #schema.first() p
            cross apply p.Scores s
            group by p.Name";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [10, 20, 30, 40, 50] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(6, table.Columns.Count());
    }

    [TestMethod]
    public void Explore14_CrossApply_AggregateWithoutGroupBy_ShouldWork()
    {
        const string query = @"
            select
                Count(s.Value) as TotalScores
            from #schema.first() p
            cross apply p.Scores s";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [1, 2, 3] },
            new() { Name = "Jane", Age = 25, Scores = [4, 5] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(5L, (long)table[0].Values[0]);
    }

    #endregion
}
