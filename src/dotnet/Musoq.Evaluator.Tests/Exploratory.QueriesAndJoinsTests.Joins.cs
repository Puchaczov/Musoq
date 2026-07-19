using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryQueriesAndJoinsTests
{
    #region Exploration 23: Multiple Joins with Cross Apply

    [TestMethod]
    public void Explore23_TwoJoins_ThenCrossApply_ShouldWork()
    {
        const string query = @"
            select p.Name, o.OrderId, t.Value
            from #schema.first() p
            inner join #schema.second() o on p.Name = o.CustomerName
            inner join #schema.second() o2 on o.OrderId = o2.OrderId
            cross apply p.Tags t";

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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Name", typeof(string)), ("o.OrderId", typeof(int)), ("t.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", 1, "vip"]);
    }

    [TestMethod]
    public void Explore23_CrossApply_ThenTwoJoins_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value, o.OrderId, o2.Total
            from #schema.first() p
            cross apply p.Tags t
            inner join #schema.second() o on p.Name = o.CustomerName
            inner join #schema.second() o2 on o.OrderId = o2.OrderId";

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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Name", typeof(string)), ("t.Value", typeof(string)), ("o.OrderId", typeof(int)),
            ("o2.Total", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "vip", 1, 100m]);
    }

    #endregion

    #region Exploration 31: Left Join Edge Cases

    [TestMethod]
    public void Explore31_LeftJoin_NoMatchingRows_ShouldReturnNull()
    {
        const string query = @"
            select p.Name, o.OrderId
            from #schema.first() p
            left outer join #schema.second() o on p.Name = o.CustomerName";

        var persons = new List<Person>
        {
            new() { Name = "John", Age = 30 }
        }.ToArray();

        var orders = new List<Order>
        {
            new() { OrderId = 1, CustomerName = "Jane", Total = 100 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons, orders);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("o.OrderId", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", null]);
    }

    [TestMethod]
    public void Explore31_LeftJoin_MultipleMatches_ShouldReturnAll()
    {
        const string query = @"
            select p.Name, o.OrderId
            from #schema.first() p
            left outer join #schema.second() o on p.Name = o.CustomerName";

        var persons = new List<Person>
        {
            new() { Name = "John", Age = 30 }
        }.ToArray();

        var orders = new List<Order>
        {
            new() { OrderId = 1, CustomerName = "John", Total = 100 },
            new() { OrderId = 2, CustomerName = "John", Total = 200 },
            new() { OrderId = 3, CustomerName = "John", Total = 300 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons, orders);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("o.OrderId", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", 1], ["John", 2], ["John", 3]);
    }

    #endregion

    #region Exploration 32: Right Join Edge Cases

    [TestMethod]
    public void Explore32_RightJoin_NoMatchingRows_ShouldReturnNull()
    {
        const string query = @"
            select p.Name, o.OrderId
            from #schema.first() p
            right outer join #schema.second() o on p.Name = o.CustomerName";

        var persons = new List<Person>
        {
            new() { Name = "John", Age = 30 }
        }.ToArray();

        var orders = new List<Order>
        {
            new() { OrderId = 1, CustomerName = "Jane", Total = 100 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons, orders);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("o.OrderId", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [null, 1]);
    }

    #endregion

    #region Exploration 33: Self-Join Patterns

    [TestMethod]
    public void Explore33_SelfJoin_WithDifferentAliases_ShouldWork()
    {
        const string query = @"
            select p1.Name, p2.Name
            from #schema.first() p1
            inner join #schema.first() p2 on p1.Age = p2.Age
            where p1.Name <> p2.Name";

        var persons = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Jane", Age = 30 },
            new() { Name = "Bob", Age = 25 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p1.Name", typeof(string)), ("p2.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["John", "Jane"], ["Jane", "John"]);
    }

    #endregion
}
