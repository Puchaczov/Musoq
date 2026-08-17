using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ExploratoryQueriesAndJoinsTests
{
    #region Exploration 22: Nested Properties with Methods

    [TestMethod]
    public void Explore22_CrossApply_StringMethodOnAppliedValue_ShouldWork()
    {
        const string query = @"
            select p.Name, ToUpper(t.Value) as UpperTag
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["hello", "world"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("UpperTag", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "HELLO"], ["John", "WORLD"]);
    }

    [TestMethod]
    public void Explore22_CrossApply_SubstringOnAppliedValue_ShouldWork()
    {
        const string query = @"
            select p.Name, Substring(t.Value, 0, 3) as ShortTag
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["hello", "world"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("ShortTag", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "hel"], ["John", "wor"]);
    }

    #endregion

    #region Exploration 26: Aliased Expressions

    [TestMethod]
    public void Explore26_CrossApply_SelectExpressionWithAlias_ShouldWork()
    {
        const string query = @"
            select
                p.Name as PersonName,
                t.Value as TagValue,
                p.Name + '-' + t.Value as Combined
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("PersonName", typeof(string)), ("TagValue", typeof(string)), ("Combined", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["John", "a", "John-a"], ["John", "b", "John-b"]);
    }

    #endregion

    #region Exploration 27: Empty and Single Element Arrays

    [TestMethod]
    public void Explore27_CrossApply_SingleElementArray_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["single"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("t.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "single"]);
    }

    [TestMethod]
    public void Explore27_CrossApply_MixedEmptyAndNonEmpty_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b"] },
            new() { Name = "Jane", Age = 25, Tags = [] },
            new() { Name = "Bob", Age = 35, Tags = ["c"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("t.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["John", "a"], ["John", "b"], ["Bob", "c"]);
    }

    #endregion

    #region Exploration 28: Nested CTEs with Cross Apply

    [TestMethod]
    public void Explore28_NestedCte_WithCrossApply_ShouldWork()
    {
        const string query = @"
            with level1 as (
                select p.Name as Name, p.Tags as Tags
                from #schema.first() p
            ),
            level2 as (
                select l.Name as Name, l.Tags as Tags
                from level1 l
            ),
            level3 as (
                select l.Name as Name, t.Value as Tag
                from level2 l
                cross apply l.Tags t
            )
            select Name, Tag
            from level3";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Tag", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["John", "a"], ["John", "b"]);
    }

    #endregion

    #region Exploration 37: Complex ORDER BY

    [TestMethod]
    public void Explore37_OrderBy_MultipleColumns_ShouldWork()
    {
        const string query = @"
            select p.Name, s.Value
            from #schema.first() p
            cross apply p.Scores s
            order by p.Name asc, s.Value desc";

        var source = new List<Person>
        {
            new() { Name = "Bob", Age = 25, Scores = [3, 1, 2] },
            new() { Name = "Alice", Age = 30, Scores = [6, 4, 5] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)), ("s.Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", 6], ["Alice", 5], ["Alice", 4], ["Bob", 3], ["Bob", 2], ["Bob", 1]);
    }

    [TestMethod]
    public void Explore37_OrderBy_WithExpression_ShouldWork()
    {
        const string query = @"
            select p.Name, s.Value, s.Value * 2 as DoubleScore
            from #schema.first() p
            cross apply p.Scores s
            order by s.Value * 2 desc";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [10, 20, 30] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Name", typeof(string)), ("s.Value", typeof(int)), ("DoubleScore", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["John", 30, 60], ["John", 20, 40], ["John", 10, 20]);
    }

    #endregion

    #region Exploration 38: Null Comparisons

    [TestMethod]
    public void Explore38_CrossApply_IsNullCheck_ShouldWork()
    {
        const string query = @"
            select p.Name
            from #schema.first() p
            outer apply p.Tags t
            where t.Value is null";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a"] },
            new() { Name = "Jane", Age = 25, Tags = [] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("p.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Jane"]);
    }

    #endregion

    #region Exploration 39: Complex CTE Patterns

    [TestMethod]
    public void Explore39_Cte_JoinWithCrossApply_ShouldWork()
    {
        const string query = @"
            with taggedPersons as (
                select p.Name as Name, t.Value as Tag
                from #schema.first() p
                cross apply p.Tags t
            )
            select tp.Name, tp.Tag, o.OrderId
            from taggedPersons tp
            inner join #schema.second() o on tp.Name = o.CustomerName";

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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("tp.Name", typeof(string)), ("tp.Tag", typeof(string)), ("o.OrderId", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["John", "vip", 1], ["John", "premium", 1]);
    }

    #endregion

    #region Exploration 40: Edge Case Data Types

    [TestMethod]
    public void Explore40_CrossApply_WithDecimalValues_ShouldWork()
    {
        const string query = @"
            select o.OrderId, i.ProductName, i.Price * i.Quantity as ItemTotal
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
                    new OrderItem { ProductName = "Widget", Quantity = 2, Price = 25.50m },
                    new OrderItem { ProductName = "Gadget", Quantity = 3, Price = 15.75m }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, orders);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("o.OrderId", typeof(int)), ("i.ProductName", typeof(string)), ("ItemTotal", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Widget", 51m], [1, "Gadget", 47.25m]);
    }

    #endregion

    #region Exploration 48: Negative numbers and arithmetic

    [TestMethod]
    public void Explore48_CrossApply_NegativeNumbers_ShouldWork()
    {
        const string query = @"
            select
                i.ProductName,
                -i.Price as NegativePrice,
                i.Price * -1 as AlsoNegative
            from #schema.first() o
            cross apply o.Items i";

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items = [new OrderItem { ProductName = "Widget", Price = 10.00m, Quantity = 1 }]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("i.ProductName", typeof(string)), ("NegativePrice", typeof(decimal)), ("AlsoNegative", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Widget", -10m, -10m]);
    }

    #endregion
}
