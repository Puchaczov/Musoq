using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryCrossApplyBasicsTests
{
    #region Exploration 3: Cross Apply with Where Clause

    [TestMethod]
    public void Explore3_CrossApply_WithWhereOnAppliedAlias_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            cross apply p.Tags t
            where t.Value = 'important'";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["normal", "important", "other"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void Explore3_CrossApply_WithWhereOnMultipleAliases_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value, s.Value
            from #schema.first() p
            cross apply p.Tags t
            cross apply p.Scores s
            where t.Value = 'a' and s.Value > 2";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b"], Scores = [1, 2, 3, 4] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 4: Cross Apply with Order By

    [TestMethod]
    public void Explore4_CrossApply_WithOrderByOnAppliedAlias_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            cross apply p.Tags t
            order by t.Value asc";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["zebra", "apple", "mango"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("apple", table[0].Values[1]);
        Assert.AreEqual("mango", table[1].Values[1]);
        Assert.AreEqual("zebra", table[2].Values[1]);
    }

    [TestMethod]
    public void Explore4_CrossApply_WithOrderByOnMultipleColumns_ShouldWork()
    {
        const string query = @"
            select p.Name, s.Value
            from #schema.first() p
            cross apply p.Scores s
            order by p.Name desc, s.Value asc";

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 25, Scores = [3, 1, 2] },
            new() { Name = "Bob", Age = 30, Scores = [6, 4, 5] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(6, table.Count);
    }

    #endregion

    #region Exploration 15: Skip/Take with Cross Apply

    [TestMethod]
    public void Explore15_CrossApply_WithSkipTake_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            cross apply p.Tags t
            skip 1 take 2";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b", "c", "d"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Explore15_CrossApply_WithOrderByAndSkipTake_ShouldWork()
    {
        const string query = @"
            select p.Name, s.Value
            from #schema.first() p
            cross apply p.Scores s
            order by s.Value desc
            skip 1 take 3";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [10, 50, 30, 40, 20] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
    }

    #endregion

    #region Exploration 19: Order items cross apply

    [TestMethod]
    public void Explore19_CrossApply_OrderItems_ShouldWork()
    {
        const string query = @"
            select o.OrderId, o.CustomerName, i.ProductName, i.Quantity
            from #schema.first() o
            cross apply o.Items i";

        var orders = new List<Order>
        {
            new()
            {
                OrderId = 1,
                CustomerName = "John",
                Total = 100,
                Items =
                [
                    new OrderItem { ProductName = "Widget", Quantity = 2, Price = 25 },
                    new OrderItem { ProductName = "Gadget", Quantity = 1, Price = 50 }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, orders);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Explore19_CrossApply_OrderItemsWithAggregation_ShouldWork()
    {
        const string query = @"
            select o.OrderId, Sum(i.Quantity * i.Price) as CalculatedTotal
            from #schema.first() o
            cross apply o.Items i
            group by o.OrderId";

        var orders = new List<Order>
        {
            new()
            {
                OrderId = 1,
                CustomerName = "John",
                Total = 100,
                Items =
                [
                    new OrderItem { ProductName = "Widget", Quantity = 2, Price = 25 },
                    new OrderItem { ProductName = "Gadget", Quantity = 1, Price = 50 }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, orders);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100m, table[0].Values[1]);
    }

    #endregion

    #region Exploration 20: Null handling edge cases

    [TestMethod]
    public void Explore20_CrossApply_WithNullPropertyInNestedObject_ShouldHandle()
    {
        const string query = @"
            select p.Name, a.City
            from #schema.first() p
            outer apply p.Addresses a";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Addresses = null }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    [TestMethod]
    public void Explore20_CrossApply_WhereWithNullCheck_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            outer apply p.Tags t
            where t.Value is not null";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b"] },
            new() { Name = "Jane", Age = 25, Tags = [] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion
}
