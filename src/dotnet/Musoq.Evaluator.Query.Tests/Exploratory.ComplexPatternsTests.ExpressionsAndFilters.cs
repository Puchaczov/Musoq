using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryComplexPatternsTests
{
    [TestMethod]
    public void Explore75_CrossApplyWithParentFilter_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Name,\n"
            + "                t.Value\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Tags t\n"
            + "            where p.Age > 25 and t.Value like 'a%'"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["admin", "user"],
            },
            new()
            {
                Name = "Jane",
                Age = 20,
                Tags = ["admin", "viewer"],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "admin"]);
    }

    [TestMethod]
    public void Explore76_MultipleStringFunctions_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Name,\n"
            + "                ToUpper(p.Name) as Upper,\n"
            + "                ToLower(p.Name) as Lower,\n"
            + "                Reverse(p.Name) as Reversed\n"
            + "            from #schema.first() p"
        );

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Upper", typeof(string)),
            ("Lower", typeof(string)),
            ("Reversed", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "JOHN", "john", "nhoJ"]);
    }

    [TestMethod]
    public void Explore78_OrConditionWithCrossApply_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Name,\n"
            + "                t.Value\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Tags t\n"
            + "            where t.Value = 'admin' or p.Age < 25"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["admin", "user"],
            },
            new()
            {
                Name = "Jane",
                Age = 20,
                Tags = ["viewer"],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "admin"], ["Jane", "viewer"]);
    }

    [TestMethod]
    public void Explore79_NestedCaseExpressions_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Name,\n"
            + "                case\n"
            + "                    when p.Age >= 60 then 'Retired'\n"
            + "                    when p.Age >= 30 then\n"
            + "                        case when p.Age >= 40 then 'Senior' else 'Adult' end\n"
            + "                    else 'Young'\n"
            + "                end as Category\n"
            + "            from #schema.first() p"
        );

        var source = new List<Person>
        {
            new() { Name = "John", Age = 35 },
            new() { Name = "Jane", Age = 45 },
            new() { Name = "Bob", Age = 65 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Category", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["John", "Adult"],
            ["Jane", "Senior"],
            ["Bob", "Retired"]
        );
    }

    [TestMethod]
    public void Explore80_CrossApplyAfterLeftJoin_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Name,\n"
            + "                o.OrderId,\n"
            + "                t.Value\n"
            + "            from #schema.first() p\n"
            + "            left outer join #schema.second() o on p.Name = o.CustomerName\n"
            + "            cross apply p.Tags t"
        );

        var persons = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["a", "b"],
            },
        }.ToArray();

        var orders = new List<Order>
        {
            new() { OrderId = 1, CustomerName = "John" },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons, orders);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("OrderId", typeof(int?)),
            ("Value", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", 1, "a"], ["John", 1, "b"]);
    }

    [TestMethod]
    public void Explore81_CrossApply_EmptySource_ShouldReturnEmpty()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Name,\n"
            + "                t.Value\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Tags t"
        );

        var source = new List<Person>().ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table);
    }

    [TestMethod]
    public void Explore82_WhereWithParentheses_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select p.Name\n"
            + "            from #schema.first() p\n"
            + "            where (p.Age > 25 and p.Age < 35) or p.Name = 'Bob'"
        );

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Jane", Age = 45 },
            new() { Name = "Bob", Age = 50 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John"], ["Bob"]);
    }

    [TestMethod]
    public void Explore83_AggregateWithoutGroupBy_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                Count(p.Name) as TotalPeople,\n"
            + "                Sum(p.Age) as TotalAge\n"
            + "            from #schema.first() p"
        );

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Jane", Age = 25 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("TotalPeople", typeof(long)), ("TotalAge", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2L, 55]);
    }

    [TestMethod]
    public void Explore84_SimpleGroupBy_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Age,\n"
            + "                Count(p.Name) as PersonCount\n"
            + "            from #schema.first() p\n"
            + "            group by p.Age"
        );

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Jane", Age = 30 },
            new() { Name = "Bob", Age = 25 },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Age", typeof(int)), ("PersonCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [30, 2L], [25, 1L]);
    }

    [TestMethod]
    public void Explore85_CrossApply_OrderByAppliedColumn_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                o.OrderId,\n"
            + "                i.ProductName\n"
            + "            from #schema.first() o\n"
            + "            cross apply o.Items i\n"
            + "            order by i.ProductName desc"
        );

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items =
                [
                    new OrderItem
                    {
                        ProductName = "Apple",
                        Price = 1m,
                        Quantity = 1,
                    },
                    new OrderItem
                    {
                        ProductName = "Zebra",
                        Price = 2m,
                        Quantity = 1,
                    },
                ],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("OrderId", typeof(int)), ("ProductName", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Zebra"], [1, "Apple"]);
    }

    [TestMethod]
    public void Explore86_MultipleCrossApplies_WithAggregation_ShouldWork()
    {
        const string query = (
            "\n"
            + "            select\n"
            + "                p.Name,\n"
            + "                Count(t.Value) as TagCount,\n"
            + "                Count(a.City) as AddressCount\n"
            + "            from #schema.first() p\n"
            + "            cross apply p.Tags t\n"
            + "            cross apply p.Addresses a\n"
            + "            group by p.Name"
        );

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["a", "b"],
                Addresses = [new Address { City = "NYC" }],
            },
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("TagCount", typeof(long)),
            ("AddressCount", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", 2L, 2L]);
    }
}
