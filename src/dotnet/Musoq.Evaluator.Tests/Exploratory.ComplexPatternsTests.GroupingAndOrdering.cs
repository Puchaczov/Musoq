using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryComplexPatternsTests
{
    [TestMethod]
    public void Explore63_MultipleCaseWhen_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                case when p.Age > 50 then 'Senior' when p.Age > 30 then 'Middle' else 'Young' end as AgeGroup,
                case when p.Tags is not null then 'Has Tags' else 'No Tags' end as TagStatus
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 55, Tags = ["a"] },
            new() { Name = "Jane", Age = 25, Tags = null }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Name", typeof(string)), ("AgeGroup", typeof(string)), ("TagStatus", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["John", "Senior", "Has Tags"], ["Jane", "Young", "No Tags"]);
    }



    [TestMethod]
    public void Explore64_OrderByWithCrossApplyResult_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t
            order by t.Value asc";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["zebra", "apple", "mango"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("t.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["John", "apple"], ["John", "mango"], ["John", "zebra"]);
    }



    [TestMethod]
    public void Explore65_ComplexJoinCondition_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                o.OrderId
            from #schema.first() p
            inner join #schema.second() o on p.Name = o.CustomerName and p.Age > 20";

        var persons = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Jane", Age = 18 }
        }.ToArray();

        var orders = new List<Order>
        {
            new() { OrderId = 1, CustomerName = "John" },
            new() { OrderId = 2, CustomerName = "Jane" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons, orders);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("o.OrderId", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", 1]);
    }



    [TestMethod]
    public void Explore66_CountDistinct_WithCrossApply_ShouldWork()
    {
        const string query = @"
            select
                Count(distinct t.Value) as UniqueTagCount
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b", "a"] },
            new() { Name = "Jane", Age = 25, Tags = ["b", "c"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("UniqueTagCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [3L]);
    }



    [TestMethod]
    public void Explore67_CrossApply_ArrayOfObjects_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                a.Street,
                a.City
            from #schema.first() p
            cross apply p.Addresses a";

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Addresses =
                [
                    new Address { Street = "123 Main", City = "NYC" },
                    new Address { Street = "456 Oak", City = "LA" }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Name", typeof(string)), ("a.Street", typeof(string)), ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["John", "123 Main", "NYC"], ["John", "456 Oak", "LA"]);
    }



    [TestMethod]
    public void Explore68_TakeWithCrossApply_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t
            take 2";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b", "c", "d"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("t.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "a"], ["John", "b"]);
    }



    [TestMethod]
    public void Explore69_SkipTakeWithCrossApply_ShouldWork()
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

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("t.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "b"], ["John", "c"]);
    }



    [TestMethod]
    public void Explore70_AliasedAggregateInHaving_ShouldWork()
    {
        const string query = @"
            select
                p.Age,
                Count(p.Name) as NameCount
            from #schema.first() p
            group by p.Age
            having Count(p.Name) >= 2";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Jane", Age = 30 },
            new() { Name = "Bob", Age = 25 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Age", typeof(int)), ("NameCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [30, 2L]);
    }



    [TestMethod]
    public void Explore71_SubqueryLikePattern_InnerJoinForExists_ShouldWork()
    {
        const string query = @"
            select distinct p.Name
            from #schema.first() p
            inner join #schema.second() o on p.Name = o.CustomerName";

        var persons = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Jane", Age = 25 },
            new() { Name = "Bob", Age = 35 }
        }.ToArray();

        var orders = new List<Order>
        {
            new() { OrderId = 1, CustomerName = "John" },
            new() { OrderId = 2, CustomerName = "John" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons, orders);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John"]);
    }



    [TestMethod]
    public void Explore72_ComplexExpressionInGroupBy_ShouldWork()
    {
        const string query = @"
            select
                p.Age / 10 * 10 as AgeDecade,
                Count(p.Name) as PersonCount
            from #schema.first() p
            group by p.Age / 10 * 10";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 31 },
            new() { Name = "Jane", Age = 35 },
            new() { Name = "Bob", Age = 42 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("AgeDecade", typeof(int)), ("PersonCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [30, 2L], [40, 1L]);
    }



    [TestMethod]
    public void Explore73_OrderByMultipleDirections_ShouldWork()
    {
        const string query = @"
            select p.Name, p.Age
            from #schema.first() p
            order by p.Age desc, p.Name asc";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Anna", Age = 30 },
            new() { Name = "Bob", Age = 25 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("p.Age", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Anna", 30], ["John", 30], ["Bob", 25]);
    }



    [TestMethod]
    public void Explore74_ArithmeticInWhere_ShouldWork()
    {
        const string query = @"
            select
                i.ProductName,
                i.Price * i.Quantity as Total
            from #schema.first() o
            cross apply o.Items i
            where i.Price * i.Quantity > 40";

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items =
                [
                    new OrderItem { ProductName = "Cheap", Price = 5m, Quantity = 2 },
                    new OrderItem { ProductName = "Expensive", Price = 15m, Quantity = 3 }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("i.ProductName", typeof(string)), ("Total", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Expensive", 45m]);
    }



}
