using System.Collections.Generic;
using System.Linq;
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

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(2, table.Columns.Count());
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

        Assert.IsNotNull(table);
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

        Assert.IsNotNull(table);

        var foundCombined = table.Any(row => row.Values.Any(val => val?.ToString() == "John-a"));
        Assert.IsTrue(foundCombined, "John-a should be in the result");
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

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
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

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
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

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
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

        Assert.IsNotNull(table);
        Assert.AreEqual(6, table.Count);
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

        Assert.IsNotNull(table);
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

        Assert.IsNotNull(table);
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

        Assert.IsNotNull(table);
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

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
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

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion
}
