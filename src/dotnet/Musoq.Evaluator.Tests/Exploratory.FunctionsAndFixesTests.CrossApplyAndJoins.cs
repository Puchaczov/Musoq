using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryFunctionsAndFixesTests : ExploratoryEvaluatorTestsBase
{
    #region Exploration 91: Cross apply with all null arrays

    [TestMethod]
    public void Explore91_OuterApply_AllNullArrays_ShouldReturnPersonsWithNulls()
    {
        const string query = @"
            select
                p.Name,
                t.Value
            from #schema.first() p
            outer apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = null },
            new() { Name = "Jane", Age = 25, Tags = null }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 93: Sum with cross apply decimal values

    [TestMethod]
    public void Explore93_SumWithCrossApply_Decimals_ShouldWork()
    {
        const string query = @"
            select
                o.OrderId,
                Sum(i.Price * i.Quantity) as Total
            from #schema.first() o
            cross apply o.Items i
            group by o.OrderId";

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items =
                [
                    new OrderItem { ProductName = "A", Price = 10.50m, Quantity = 2 },
                    new OrderItem { ProductName = "B", Price = 5.25m, Quantity = 4 }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(42m, table[0].Values[1]);
    }

    #endregion

    #region Exploration 94: Avg with cross apply

    [TestMethod]
    public void Explore94_AvgWithCrossApply_ShouldWork()
    {
        const string query = @"
            select
                o.OrderId,
                Avg(i.Price) as AvgPrice
            from #schema.first() o
            cross apply o.Items i
            group by o.OrderId";

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items =
                [
                    new OrderItem { ProductName = "A", Price = 10m, Quantity = 1 },
                    new OrderItem { ProductName = "B", Price = 20m, Quantity = 1 }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Exploration 97: Cross apply with single row source

    [TestMethod]
    public void Explore97_CrossApply_SingleRowSource_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "Only", Age = 30, Tags = ["one", "two", "three"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
    }

    #endregion

    #region Exploration 98: Join with OR condition

    [TestMethod]
    public void Explore98_JoinWithOrCondition_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                o.OrderId
            from #schema.first() p
            inner join #schema.second() o on p.Name = o.CustomerName or p.Age = o.OrderId";

        var persons = new List<Person>
        {
            new() { Name = "John", Age = 2 },
            new() { Name = "Jane", Age = 999 }
        }.ToArray();

        var orders = new List<Order>
        {
            new() { OrderId = 1, CustomerName = "John" },
            new() { OrderId = 2, CustomerName = "Bob" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons, orders);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 103: Cross apply with WHERE on both

    [TestMethod]
    public void Explore103_CrossApply_WhereOnBothSources_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t
            where p.Age > 25 and t.Value like '%a%'";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["admin", "xyz"] },
            new() { Name = "Jane", Age = 20, Tags = ["admin", "data"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Exploration 107: Cross apply with computed filter

    [TestMethod]
    public void Explore107_CrossApply_ComputedFilter_ShouldWork()
    {
        const string query = @"
            select
                o.OrderId,
                i.ProductName,
                i.Price * i.Quantity as LineTotal
            from #schema.first() o
            cross apply o.Items i
            where i.Price * i.Quantity >= 20";

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items =
                [
                    new OrderItem { ProductName = "Small", Price = 5m, Quantity = 2 },
                    new OrderItem { ProductName = "Big", Price = 10m, Quantity = 3 }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Exploration 108: Cross apply with Skip

    [TestMethod]
    public void Explore108_CrossApply_Skip_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t
            skip 2";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b", "c", "d"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 109: Cross apply with Skip and Take

    [TestMethod]
    public void Explore109_CrossApply_SkipAndTake_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value
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

    #endregion
}
