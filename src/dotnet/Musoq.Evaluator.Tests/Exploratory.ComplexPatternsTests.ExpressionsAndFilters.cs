using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryComplexPatternsTests
{
    [TestMethod]
    public void Explore75_CrossApplyWithParentFilter_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t
            where p.Age > 25 and t.Value like 'a%'";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["admin", "user"] },
            new() { Name = "Jane", Age = 20, Tags = ["admin", "viewer"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }



    [TestMethod]
    public void Explore76_MultipleStringFunctions_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                ToUpper(p.Name) as Upper,
                ToLower(p.Name) as Lower,
                Reverse(p.Name) as Reversed
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
    public void Explore77_DateFunctions_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                GetDate() as CurrentDate,
                Year(GetDate()) as CurrentYear
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
    public void Explore78_OrConditionWithCrossApply_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t
            where t.Value = 'admin' or p.Age < 25";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["admin", "user"] },
            new() { Name = "Jane", Age = 20, Tags = ["viewer"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }



    [TestMethod]
    public void Explore79_NestedCaseExpressions_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                case
                    when p.Age >= 60 then 'Retired'
                    when p.Age >= 30 then
                        case when p.Age >= 40 then 'Senior' else 'Adult' end
                    else 'Young'
                end as Category
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 35 },
            new() { Name = "Jane", Age = 45 },
            new() { Name = "Bob", Age = 65 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
    }



    [TestMethod]
    public void Explore80_CrossApplyAfterLeftJoin_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                o.OrderId,
                t.Value
            from #schema.first() p
            left outer join #schema.second() o on p.Name = o.CustomerName
            cross apply p.Tags t";

        var persons = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b"] }
        }.ToArray();

        var orders = new List<Order>
        {
            new() { OrderId = 1, CustomerName = "John" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons, orders);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }



    [TestMethod]
    public void Explore81_CrossApply_EmptySource_ShouldReturnEmpty()
    {
        const string query = @"
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>().ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(0, table.Count);
    }



    [TestMethod]
    public void Explore82_WhereWithParentheses_ShouldWork()
    {
        const string query = @"
            select p.Name
            from #schema.first() p
            where (p.Age > 25 and p.Age < 35) or p.Name = 'Bob'";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Jane", Age = 45 },
            new() { Name = "Bob", Age = 50 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }



    [TestMethod]
    public void Explore83_AggregateWithoutGroupBy_ShouldWork()
    {
        const string query = @"
            select
                Count(p.Name) as TotalPeople,
                Sum(p.Age) as TotalAge
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Jane", Age = 25 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }



    [TestMethod]
    public void Explore84_SimpleGroupBy_ShouldWork()
    {
        const string query = @"
            select
                p.Age,
                Count(p.Name) as PersonCount
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
    public void Explore85_CrossApply_OrderByAppliedColumn_ShouldWork()
    {
        const string query = @"
            select
                o.OrderId,
                i.ProductName
            from #schema.first() o
            cross apply o.Items i
            order by i.ProductName desc";

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items =
                [
                    new OrderItem { ProductName = "Apple", Price = 1m, Quantity = 1 },
                    new OrderItem { ProductName = "Zebra", Price = 2m, Quantity = 1 }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Zebra", table[0].Values[1]);
    }



    [TestMethod]
    public void Explore86_MultipleCrossApplies_WithAggregation_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                Count(t.Value) as TagCount,
                Count(a.City) as AddressCount
            from #schema.first() p
            cross apply p.Tags t
            cross apply p.Addresses a
            group by p.Name";

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["a", "b"],
                Addresses = [new Address { City = "NYC" }]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }



}
