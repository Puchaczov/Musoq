using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryCrossApplyBasicsTests
{
    #region Exploration 8: CTE with Cross Apply

    [TestMethod]
    public void Explore8_Cte_ThenCrossApply_ShouldWork()
    {
        const string query = @"
            with cte as (
                select p.Name as Name, p.Tags as Tags
                from #schema.first() p
            )
            select c.Name, t.Value
            from cte c
            cross apply c.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    [TestMethod]
    public void Explore8_Cte_WithMultipleCrossApplies_ShouldWork()
    {
        const string query = @"
            with cte as (
                select p.Name as Name, p.Tags as Tags, p.Scores as Scores
                from #schema.first() p
            )
            select c.Name, t.Value, s.Value
            from cte c
            cross apply c.Tags t
            cross apply c.Scores s";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a"], Scores = [1, 2] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    [TestMethod]
    public void Explore8_MultipleCtes_WithCrossApply_ShouldWork()
    {
        const string query = @"
            with cte1 as (
                select p.Name as Name, p.Tags as Tags
                from #schema.first() p
            ),
            cte2 as (
                select c.Name as PersonName, t.Value as TagValue
                from cte1 c
                cross apply c.Tags t
            )
            select PersonName, TagValue
            from cte2";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 9: Subqueries (Parser limitations - subqueries not fully supported)

    [TestMethod]
    public void Explore9_CrossApply_InSubquery_ShouldWork()
    {
        const string query = @"
            select *
            from (
                select p.Name, t.Value
                from #schema.first() p
                cross apply p.Tags t
            ) sub";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    [TestMethod]
    public void Explore9_SubqueryWithCrossApply_JoinedWithTable_ShouldWork()
    {
        const string query = @"
            select sub.Name, sub.Tag, o.OrderId
            from (
                select p.Name as Name, t.Value as Tag
                from #schema.first() p
                cross apply p.Tags t
            ) sub
            inner join #schema.second() o on sub.Name = o.CustomerName";

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

    #endregion

    #region Exploration 11: Distinct with Cross Apply

    [TestMethod]
    public void Explore11_CrossApply_WithDistinct_ShouldWork()
    {
        const string query = @"
            select distinct t.Value
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b", "a"] },
            new() { Name = "Jane", Age = 25, Tags = ["b", "c"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void Explore11_CrossApply_DistinctOnMultipleColumns_ShouldWork()
    {
        const string query = @"
            select distinct p.Name, t.Value
            from #schema.first() p
            cross apply p.Tags t
            cross apply p.Tags t2";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    #endregion
}
