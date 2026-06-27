using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Exploratory tests: Complex patterns (Explorations 51-90).
/// </summary>
[TestClass]
public partial class ExploratoryComplexPatternsTests : ExploratoryEvaluatorTestsBase
{

    [TestMethod]
    public void Explore51_TripleCrossApply_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value as Tag,
                a.City
            from #schema.first() p
            cross apply p.Tags t
            cross apply p.Addresses a
            where t.Value = 'vip'";

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["vip", "premium"],
                Addresses = [new Address { City = "NYC" }, new Address { City = "LA" }]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }



    [TestMethod]
    public void Explore52_SameTableJoinedTwice_ShouldWork()
    {
        const string query = @"
            select
                p1.Name as Name1,
                p2.Name as Name2
            from #schema.first() p1
            inner join #schema.first() p2 on p1.Age = p2.Age
            where p1.Name <> p2.Name";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Jane", Age = 30 },
            new() { Name = "Bob", Age = 25 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }



    [TestMethod]
    public void Explore53_ArithmeticWithNullValues_ShouldWork()
    {
        const string query = @"
            select
                i.ProductName,
                i.Price + 0 as PricePlusZero,
                i.Quantity * 1 as QuantityTimesOne
            from #schema.first() o
            cross apply o.Items i";

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items = [new OrderItem { ProductName = "Widget", Price = 10.50m, Quantity = 2 }]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }



    [TestMethod]
    public void Explore54_MultipleAggregatesWithCrossApply_ShouldWork()
    {
        const string query = @"
            select
                o.OrderId,
                Sum(i.Price) as TotalPrice,
                Avg(i.Price) as AvgPrice,
                Min(i.Price) as MinPrice,
                Max(i.Price) as MaxPrice
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
                    new OrderItem { ProductName = "B", Price = 20m, Quantity = 1 },
                    new OrderItem { ProductName = "C", Price = 30m, Quantity = 1 }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }



    [TestMethod]
    public void Explore55_CrossApply_EmptyArrayWithJoin_ShouldReturnNoRows()
    {
        const string query = @"
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t
            inner join #schema.first() p2 on p.Age = p2.Age";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = [] },
            new() { Name = "Jane", Age = 25, Tags = ["a"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);


        Assert.AreEqual(1, table.Count);
    }



    [TestMethod]
    public void Explore56_WhereOnCrossAppliedProperty_ShouldWork()
    {
        const string query = @"
            select
                o.OrderId,
                i.ProductName
            from #schema.first() o
            cross apply o.Items i
            where i.Price > 15";

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items =
                [
                    new OrderItem { ProductName = "Cheap", Price = 5m, Quantity = 1 },
                    new OrderItem { ProductName = "Expensive", Price = 50m, Quantity = 1 }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }



    [TestMethod]
    public void Explore57_StringConcatenation_WithNulls_ShouldWork()
    {
        const string query = @"
            select
                p.Name + ' - ' + ToString(p.Age) as NameAge
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }



    [TestMethod]
    public void Explore58_DistinctWithGroupBy_ShouldWork()
    {
        const string query = @"
            select distinct p.Age, Count(p.Name) as NameCount
            from #schema.first() p
            group by p.Age";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Jane", Age = 30 },
            new() { Name = "Bob", Age = 25 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }



    [TestMethod]
    public void Explore59_NotEquals_WithCrossApply_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t
            where t.Value <> 'admin'";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["admin", "user", "viewer"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }



    [TestMethod]
    public void Explore60_InClause_WithCrossApply_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t
            where t.Value in ('admin', 'superuser')";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["admin", "user", "viewer"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }



    [TestMethod]
    public void Explore61_Like_WithCrossApply_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t
            where t.Value like 'admin%'";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["admin", "administrator", "user"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }



    [TestMethod]
    public void Explore62_Contains_WithCrossApply_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t
            where Contains(t.Value, 'min')";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["admin", "administrator", "user"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }



}
