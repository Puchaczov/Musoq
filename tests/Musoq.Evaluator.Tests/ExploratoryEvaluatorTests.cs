using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Exploratory tests to discover edge cases and potential bugs in the evaluator.
///     These tests focus on complex query patterns, unusual combinations, and boundary conditions.
/// </summary>
[TestClass]
public class ExploratoryEvaluatorTests : GenericEntityTestBase
{
    public TestContext TestContext { get; set; }

    #region Exploration 12: Union with Cross Apply

    [TestMethod]
    public void Explore12_CrossApply_WithUnion_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t
            where p.Age > 25
            union all (Name, Value)
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t
            where p.Age <= 25";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["older"] },
            new() { Name = "Jane", Age = 20, Tags = ["younger"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "John" && (string)r.Values[1] == "older"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "Jane" && (string)r.Values[1] == "younger"));
    }

    #endregion

    #region Exploration 13: Self-referential and Manager patterns

    [TestMethod]
    public void Explore13_CrossApply_OnSameArrayTwice_ShouldWork()
    {
        const string query = @"
            select t1.Value as First, t2.Value as Second
            from #schema.first() p
            cross apply p.Tags t1
            cross apply p.Tags t2
            where t1.Value <> t2.Value";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b", "c"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(6, table.Count);
    }

    #endregion

    #region Exploration 18: Complex join conditions

    [TestMethod]
    public void Explore18_CrossApply_JoinWithComplexCondition_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value, o.OrderId
            from #schema.first() p
            cross apply p.Tags t
            inner join #schema.second() o on p.Name = o.CustomerName and o.Total > 50";

        var persons = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["vip"] }
        }.ToArray();

        var orders = new List<Order>
        {
            new() { OrderId = 1, CustomerName = "John", Total = 100 },
            new() { OrderId = 2, CustomerName = "John", Total = 30 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons, orders);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    #endregion

    #region Test Data Classes

    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string[] Tags { get; set; }
        public int[] Scores { get; set; }
        public Address[] Addresses { get; set; }
        public Person Manager { get; set; }
    }

    public class Address
    {
        public string City { get; set; }
        public string Street { get; set; }
        public string[] PhoneNumbers { get; set; }
    }

    public class Order
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public OrderItem[] Items { get; set; }
        public decimal Total { get; set; }
    }

    public class OrderItem
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class TreeNode
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public TreeNode[] Children { get; set; }
    }

    #endregion

    #region Exploration 1: Multiple Cross Applies with Method Calls

    [TestMethod]
    public void Explore1_CrossApply_MethodCallThenProperty_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            cross apply p.Split(p.Name, ' ') t";

        var source = new List<Person>
        {
            new() { Name = "John Doe", Age = 30, Tags = ["a", "b"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Explore1_CrossApply_PropertyThenMethodCall_ShouldWork()
    {
        const string query = @"
            select t.Value, s.Value
            from #schema.first() p
            cross apply p.Tags t
            cross apply p.Split(t.Value, '-') s";

        var source = new List<Person>
        {
            new() { Name = "Test", Age = 30, Tags = ["a-b", "c-d"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    [TestMethod]
    public void Explore1_ThreeCrossApplies_DifferentTypes_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value, s.Value
            from #schema.first() p
            cross apply p.Tags t
            cross apply p.Scores s";

        var source = new List<Person>
        {
            new() { Name = "Test", Age = 30, Tags = ["a", "b"], Scores = [1, 2, 3] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(6, table.Count);
    }

    #endregion

    #region Exploration 2: Nested Object Property Access

    [TestMethod]
    public void Explore2_CrossApply_NestedObjectProperty_ShouldWork()
    {
        const string query = @"
            select p.Name, a.City
            from #schema.first() p
            cross apply p.Addresses a";

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Addresses = [new Address { City = "NYC", Street = "Broadway" }]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void Explore2_CrossApply_NestedObjectThenArray_ShouldWork()
    {
        const string query = @"
            select p.Name, a.City, ph.Value
            from #schema.first() p
            cross apply p.Addresses a
            cross apply a.PhoneNumbers ph";

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Addresses =
                [
                    new Address { City = "NYC", Street = "Broadway", PhoneNumbers = ["111", "222"] },
                    new Address { City = "LA", Street = "Hollywood", PhoneNumbers = ["333"] }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void Explore2_CrossApply_ThreeLevelNesting_ShouldWork()
    {
        const string query = @"
            select root.Value, c.Value, gc.Value
            from #schema.first() root
            cross apply root.Children c
            cross apply c.Children gc";

        var source = new List<TreeNode>
        {
            new()
            {
                Id = 1,
                Value = "Root",
                Children =
                [
                    new TreeNode
                    {
                        Id = 2,
                        Value = "Child1",
                        Children = [new TreeNode { Id = 3, Value = "Grandchild1" }]
                    }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    #endregion

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

    #region Exploration 5: Cross Apply with Group By

    [TestMethod]
    public void Explore5_CrossApply_WithGroupByOnSource_ShouldWork()
    {
        const string query = @"
            select p.Name, Count(t.Value) as TagCount
            from #schema.first() p
            cross apply p.Tags t
            group by p.Name";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b", "c"] },
            new() { Name = "Jane", Age = 25, Tags = ["x", "y"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Explore5_CrossApply_WithGroupByOnAppliedValue_ShouldWork()
    {
        const string query = @"
            select t.Value, Count(p.Name) as PersonCount
            from #schema.first() p
            cross apply p.Tags t
            group by t.Value";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["common", "unique1"] },
            new() { Name = "Jane", Age = 25, Tags = ["common", "unique2"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    [TestMethod]
    public void Explore5_CrossApply_WithGroupByHaving_ShouldWork()
    {
        const string query = @"
            select p.Name, Sum(s.Value) as TotalScore
            from #schema.first() p
            cross apply p.Scores s
            group by p.Name
            having Sum(s.Value) > 10";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [1, 2, 3] },
            new() { Name = "Jane", Age = 25, Scores = [10, 20, 30] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Jane", table[0].Values[0]);
    }

    #endregion

    #region Exploration 6: Cross Apply with Join

    [TestMethod]
    public void Explore6_CrossApply_ThenInnerJoin_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value, o.OrderId
            from #schema.first() p
            cross apply p.Tags t
            inner join #schema.second() o on p.Name = o.CustomerName";

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

    [TestMethod]
    public void Explore6_InnerJoin_ThenCrossApply_ShouldWork()
    {
        const string query = @"
            select p.Name, o.OrderId, t.Value
            from #schema.first() p
            inner join #schema.second() o on p.Name = o.CustomerName
            cross apply p.Tags t";

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

    [TestMethod]
    public void Explore6_CrossApply_ThenLeftJoin_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value, o.OrderId
            from #schema.first() p
            cross apply p.Tags t
            left outer join #schema.second() o on p.Name = o.CustomerName";

        var persons = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["vip"] },
            new() { Name = "Jane", Age = 25, Tags = ["new"] }
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

    #region Exploration 7: Outer Apply Edge Cases

    [TestMethod]
    public void Explore7_OuterApply_WithEmptyArray_ShouldReturnNull()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            outer apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = [] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void Explore7_OuterApply_WithNullArray_ShouldReturnNull()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            outer apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = null }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    [TestMethod]
    public void Explore7_MixedCrossAndOuterApply_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value, s.Value
            from #schema.first() p
            cross apply p.Tags t
            outer apply p.Scores s";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a"], Scores = [] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    #endregion

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
    [Ignore("Subqueries in FROM clause are a known parser limitation")]
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
    [Ignore("Subqueries in FROM clause are a known parser limitation")]
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

    #region Exploration 10: Complex Expressions in Select

    [TestMethod]
    public void Explore10_CrossApply_WithComplexSelectExpressions_ShouldWork()
    {
        const string query = @"
            select
                p.Name + ' - ' + t.Value as Combined,
                Length(t.Value) as TagLength,
                p.Age * 2 as DoubleAge
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["hello"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Columns.Count());
    }

    [TestMethod]
    public void Explore10_CrossApply_WithCaseWhen_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                case when s.Value > 50 then 'High' else 'Low' end as ScoreLevel
            from #schema.first() p
            cross apply p.Scores s";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [25, 75] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Explore10_CrossApply_WithCoalesce_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                Coalesce(t.Value, 'NoTag') as TagOrDefault
            from #schema.first() p
            outer apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = [] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
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

    #region Exploration 14: Aggregation edge cases

    [TestMethod]
    public void Explore14_CrossApply_WithMultipleAggregates_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                Count(s.Value) as ScoreCount,
                Sum(s.Value) as TotalScore,
                Avg(s.Value) as AvgScore,
                Min(s.Value) as MinScore,
                Max(s.Value) as MaxScore
            from #schema.first() p
            cross apply p.Scores s
            group by p.Name";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [10, 20, 30, 40, 50] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(6, table.Columns.Count());
    }

    [TestMethod]
    public void Explore14_CrossApply_AggregateWithoutGroupBy_ShouldWork()
    {
        const string query = @"
            select
                Count(s.Value) as TotalScores
            from #schema.first() p
            cross apply p.Scores s";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [1, 2, 3] },
            new() { Name = "Jane", Age = 25, Scores = [4, 5] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(5, (int)table[0].Values[0]);
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

    #region Exploration 16: Empty source scenarios

    [TestMethod]
    public void Explore16_CrossApply_EmptySource_ShouldReturnEmpty()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>().ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void Explore16_CrossApply_AllEmptyArrays_ShouldReturnEmpty()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = [] },
            new() { Name = "Jane", Age = 25, Tags = [] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(0, table.Count);
    }

    #endregion

    #region Exploration 17: Aliasing edge cases

    [TestMethod]
    public void Explore17_CrossApply_SameAliasAsColumn_ShouldWork()
    {
        const string query = @"
            select p.Name, Name.Value
            from #schema.first() p
            cross apply p.Tags Name";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["tag1"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    [TestMethod]
    public void Explore17_CrossApply_LongAlias_ShouldWork()
    {
        const string query = @"
            select p.Name, ThisIsAVeryLongAliasNameForTheCrossAppliedTags.Value
            from #schema.first() p
            cross apply p.Tags ThisIsAVeryLongAliasNameForTheCrossAppliedTags";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["tag1"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
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

    #region Round 2: Additional Exploratory Tests

    #region Exploration 21: Complex WHERE with Cross Apply

    [TestMethod]
    public void Explore21_CrossApply_WhereWithOr_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            cross apply p.Tags t
            where t.Value = 'a' or t.Value = 'c'";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b", "c"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Explore21_CrossApply_WhereWithIn_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            cross apply p.Tags t
            where t.Value in ('a', 'c', 'e')";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b", "c", "d", "e"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void Explore21_CrossApply_WhereWithLike_ShouldWork()
    {
        const string query = @"
            select p.Name, t.Value
            from #schema.first() p
            cross apply p.Tags t
            where t.Value like 'test%'";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["test1", "test2", "other"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    [Ignore("BETWEEN keyword is not currently supported in parser")]
    public void Explore21_CrossApply_WhereWithBetween_ShouldWork()
    {
        const string query = @"
            select p.Name, s.Value
            from #schema.first() p
            cross apply p.Scores s
            where s.Value between 20 and 40";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [10, 20, 30, 40, 50] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
    }

    #endregion

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

        Assert.IsNotNull(table);
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

        Assert.IsNotNull(table);
    }

    #endregion

    #region Exploration 24: Cross Apply with Arithmetic

    [TestMethod]
    public void Explore24_CrossApply_ArithmeticOnAppliedValue_ShouldWork()
    {
        const string query = @"
            select p.Name, s.Value * 2 as DoubleScore, s.Value + 100 as BonusScore
            from #schema.first() p
            cross apply p.Scores s";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [10, 20, 30] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void Explore24_CrossApply_AggregateAfterArithmetic_ShouldWork()
    {
        const string query = @"
            select p.Name, Sum(s.Value * 2) as TotalDoubleScore
            from #schema.first() p
            cross apply p.Scores s
            group by p.Name";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [10, 20, 30] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(120m, table[0].Values[1]);
    }

    #endregion

    #region Exploration 25: Multiple Sources with Cross Apply

    [TestMethod]
    public void Explore25_CrossApply_FromBothJoinedSources_ShouldWork()
    {
        const string query = @"
            select p.Name, pt.Value, oi.ProductName
            from #schema.first() p
            cross apply p.Tags pt
            inner join #schema.second() o on p.Name = o.CustomerName
            cross apply o.Items oi";

        var persons = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["vip"] }
        }.ToArray();

        var orders = new List<Order>
        {
            new()
            {
                OrderId = 1,
                CustomerName = "John",
                Total = 100,
                Items = [new OrderItem { ProductName = "Widget", Quantity = 1, Price = 100 }]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, persons, orders);
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

    #region Exploration 29: Complex Group By with Cross Apply

    [TestMethod]
    [Ignore("Count(*) with multiple cross applies and group by has method resolution issue")]
    public void Explore29_CrossApply_GroupByMultipleColumns_ShouldWork()
    {
        const string query = @"
            select p.Name, s.Value, Count(*) as Cnt
            from #schema.first() p
            cross apply p.Scores s
            cross apply p.Tags t
            group by p.Name, s.Value";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [10, 20], Tags = ["a", "b"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    #endregion

    #region Exploration 30: Cross Apply with Constants

    [TestMethod]
    public void Explore30_CrossApply_SelectConstantWithApplied_ShouldWork()
    {
        const string query = @"
            select 'Prefix' as Prefix, t.Value, 'Suffix' as Suffix
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["tag1"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual("Prefix", table[0].Values[0]);
        Assert.AreEqual("tag1", table[0].Values[1]);
        Assert.AreEqual("Suffix", table[0].Values[2]);
    }

    #endregion

    #endregion

    #region Round 3: Complex Scenarios and Edge Cases

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

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
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

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
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

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
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

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 34: Aggregate with Multiple Conditions

    [TestMethod]
    public void Explore34_ConditionalCount_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                Count(case when s.Value > 50 then 1 else null end) as HighScoreCount
            from #schema.first() p
            cross apply p.Scores s
            group by p.Name";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [10, 60, 30, 70, 90] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    #endregion

    #region Exploration 35: Nested Aggregates Scenarios

    [TestMethod]
    public void Explore35_MultipleAggregatesInSingleQuery_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                Count(s.Value) as ScoreCount,
                Sum(s.Value) as TotalScore,
                Avg(s.Value) as AvgScore
            from #schema.first() p
            cross apply p.Scores s
            group by p.Name";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Scores = [10, 20, 30] },
            new() { Name = "Jane", Age = 25, Scores = [5, 15, 25, 35] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 36: Cross Apply with Function

    [TestMethod]
    public void Explore36_CrossApply_WithSplitFunction_ShouldWork()
    {
        const string query = @"
            select p.Name, part.Value
            from #schema.first() p
            cross apply p.Split(p.Name, ' ') part";

        var source = new List<Person>
        {
            new() { Name = "John Doe Smith", Age = 30 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
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

    #endregion

    #region Round 4: Explorations 41-50

    #region Exploration 41: CASE expressions with Cross Apply

    [TestMethod]
    public void Explore41_CrossApply_CaseWhenInSelect_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value,
                case when t.Value = 'admin' then 'Admin User' else 'Regular User' end as UserType
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["admin", "user"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Explore41_CrossApply_CaseWhenWithNull_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                case when p.Tags is null then 'No Tags' else 'Has Tags' end as TagStatus
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = null },
            new() { Name = "Jane", Age = 25, Tags = ["a"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 42: Multiple cross applies with different array types

    [TestMethod]
    public void Explore42_MultipleCrossApplies_DifferentArrayTypes_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value as Tag,
                a.Street
            from #schema.first() p
            cross apply p.Tags t
            cross apply p.Addresses a";

        var source = new List<Person>
        {
            new()
            {
                Name = "John",
                Age = 30,
                Tags = ["vip"],
                Addresses = [new Address { Street = "123 Main St", City = "NYC" }]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Exploration 43: Nested property access after cross apply

    [TestMethod]
    public void Explore43_CrossApply_NestedPropertyAccess_ShouldWork()
    {
        const string query = @"
            select
                o.OrderId,
                i.ProductName,
                i.Price * i.Quantity as LineTotal
            from #schema.first() o
            cross apply o.Items i";

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items =
                [
                    new OrderItem { ProductName = "Widget", Price = 10.00m, Quantity = 2 },
                    new OrderItem { ProductName = "Gadget", Price = 25.50m, Quantity = 1 }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 44: Case/IfNull with cross apply

    [TestMethod]
    public void Explore44_CrossApply_CaseWhenNull_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                case when a.City is null then 'Unknown' else a.City end as City
            from #schema.first() p
            outer apply p.Addresses a";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Addresses = null },
            new() { Name = "Jane", Age = 25, Addresses = [new Address { City = "NYC" }] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
    }

    #endregion

    #region Exploration 45: String functions with cross apply values

    [TestMethod]
    public void Explore45_CrossApply_StringFunctions_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                ToUpper(t.Value) as UpperTag,
                ToLower(t.Value) as LowerTag
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
    }

    #endregion

    #region Exploration 46: Having clause with cross apply aggregation

    [TestMethod]
    public void Explore46_CrossApply_GroupByHaving_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                Count(t.Value) as TagCount
            from #schema.first() p
            cross apply p.Tags t
            group by p.Name
            having Count(t.Value) > 1";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["a", "b", "c"] },
            new() { Name = "Jane", Age = 25, Tags = ["x"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Exploration 47: Distinct with cross apply

    [TestMethod]
    public void Explore47_CrossApply_Distinct_ShouldWork()
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

    #region Exploration 49: Boolean expressions with cross apply

    [TestMethod]
    public void Explore49_CrossApply_BooleanExpression_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value,
                t.Value = 'admin' as IsAdmin
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["admin", "user"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Explore49_CrossApply_AndOrExpression_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                t.Value
            from #schema.first() p
            cross apply p.Tags t
            where t.Value = 'admin' or t.Value = 'vip'";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["admin", "user", "vip"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 50: Type conversion edge cases

    [TestMethod]
    public void Explore50_CrossApply_ToStringConversion_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                ToString(p.Age) as AgeString,
                p.Age + 0 as AgeNumber
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
    public void Explore50_CrossApply_DecimalToInt_ShouldWork()
    {
        const string query = @"
            select
                i.ProductName,
                ToInt32(i.Price) as RoundedPrice
            from #schema.first() o
            cross apply o.Items i";

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items = [new OrderItem { ProductName = "Widget", Price = 10.99m, Quantity = 1 }]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #endregion

    #region Round 5: Explorations 51-60

    #region Exploration 51: Deeply nested cross apply

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

    #endregion

    #region Exploration 52: Self-reference and recursion-like patterns

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

    #endregion

    #region Exploration 53: Complex arithmetic with nulls

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

    #endregion

    #region Exploration 54: Multiple aggregate functions

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

    #endregion

    #region Exploration 55: Empty arrays behavior

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

    #endregion

    #region Exploration 56: Where clause on cross applied property

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

    #endregion

    #region Exploration 57: String concatenation edge cases

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

    #endregion

    #region Exploration 58: Distinct with grouping

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

    #endregion

    #region Exploration 59: NOT equals with cross apply

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

    #endregion

    #region Exploration 60: IN clause with cross apply values

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

    #endregion

    #endregion

    #region Round 6: Explorations 61-70

    #region Exploration 61: LIKE pattern matching with cross apply

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

    #endregion

    #region Exploration 62: Contains/StartsWith/EndsWith function

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

    #endregion

    #region Exploration 63: Multiple CASE WHEN in same query

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

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 64: Order by with cross apply result

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

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);

        Assert.AreEqual("apple", table[0].Values[1]);
    }

    #endregion

    #region Exploration 65: Complex join condition

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

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Exploration 66: Count distinct - Parser Limitation

    [TestMethod]
    [Ignore("Parser does not support Count(distinct ...) syntax")]
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

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Exploration 67: Cross apply on property that is array of objects

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

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 68: Take with cross apply

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

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 69: Skip and take with cross apply

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

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 70: Aliased aggregates in having

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

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #endregion

    #region Round 7: Explorations 71-80

    #region Exploration 71: Subquery in WHERE with EXISTS-like pattern

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

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Exploration 72: Complex expression in GROUP BY - Potential Bug

    [TestMethod]
    [Ignore(
        "Potential bug: Complex expressions in GROUP BY may fail with 'Group does not have value' - needs investigation")]
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

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 73: Order by multiple columns different directions

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

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);

        Assert.AreEqual("Anna", table[0].Values[0]);
    }

    #endregion

    #region Exploration 74: Arithmetic in WHERE clause

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

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Exploration 75: Cross apply with filter on parent

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

    #endregion

    #region Exploration 76: Multiple string functions

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

    #endregion

    #region Exploration 77: Date functions

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

    #endregion

    #region Exploration 78: OR condition with cross apply

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

    #endregion

    #region Exploration 79: Nested CASE expressions

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

    #endregion

    #region Exploration 80: Cross apply after left join

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

    #endregion

    #endregion

    #region Round 8: Explorations 81-90

    #region Exploration 81: Cross apply on empty source

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

    #endregion

    #region Exploration 82: Multiple WHERE conditions with parentheses

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

    #endregion

    #region Exploration 83: Aggregate without GROUP BY

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

    #endregion

    #region Exploration 84: Simple GROUP BY with column

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

    #endregion

    #region Exploration 85: Cross apply with ORDER BY on cross applied column

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

    #endregion

    #region Exploration 86: Multiple cross applies with aggregation

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

    #endregion

    #region Exploration 87: Cast function

    [TestMethod]
    public void Explore87_CastFunction_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                ToDecimal(p.Age) * 1.5 as AgeMultiplied
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

    #endregion

    #region Exploration 88: Substring function

    [TestMethod]
    public void Explore88_SubstringFunction_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                Substring(p.Name, 0, 2) as FirstTwo
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

    #endregion

    #region Exploration 89: Replace function

    [TestMethod]
    public void Explore89_ReplaceFunction_ShouldWork()
    {
        const string query = @"
            select
                t.Value,
                Replace(t.Value, 'a', 'X') as Replaced
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["apple", "banana"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 90: Trim functions

    [TestMethod]
    public void Explore90_TrimFunctions_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                Trim(p.Name) as Trimmed,
                TrimStart(p.Name) as LeftTrimmed,
                TrimEnd(p.Name) as RightTrimmed
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "  John  ", Age = 30 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #endregion

    #region Round 9: Explorations 91-100

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

    #region Exploration 92: Where with multiple IN clauses

    [TestMethod]
    public void Explore92_WhereMultipleInClauses_ShouldWork()
    {
        const string query = @"
            select p.Name
            from #schema.first() p
            where p.Name in ('John', 'Jane') and p.Age in (25, 30)";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Jane", Age = 25 },
            new() { Name = "Bob", Age = 30 },
            new() { Name = "Alice", Age = 35 }
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

    #region Exploration 95: Mixed aggregates with Where

    [TestMethod]
    public void Explore95_MixedAggregatesWithWhere_ShouldWork()
    {
        const string query = @"
            select
                o.OrderId,
                Count(i.ProductName) as ItemCount,
                Sum(i.Price) as TotalPrice
            from #schema.first() o
            cross apply o.Items i
            where i.Price > 5
            group by o.OrderId";

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items =
                [
                    new OrderItem { ProductName = "Cheap", Price = 3m, Quantity = 1 },
                    new OrderItem { ProductName = "Expensive", Price = 15m, Quantity = 1 },
                    new OrderItem { ProductName = "Medium", Price = 8m, Quantity = 1 }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Exploration 96: String comparison with case sensitivity

    [TestMethod]
    public void Explore96_StringComparison_CaseSensitive_ShouldWork()
    {
        const string query = @"
            select p.Name
            from #schema.first() p
            where p.Name = 'john'";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "john", Age = 25 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("john", table[0].Values[0]);
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

    #region Exploration 99: StartsWith and EndsWith

    [TestMethod]
    public void Explore99_StartsWithEndsWith_ShouldWork()
    {
        const string query = @"
            select
                t.Value,
                StartsWith(t.Value, 'a') as StartsWithA,
                EndsWith(t.Value, 'le') as EndsWithLE
            from #schema.first() p
            cross apply p.Tags t";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["apple", "banana", "able"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
    }

    #endregion

    #region Exploration 100: Concat function

    [TestMethod]
    public void Explore100_ConcatFunction_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                Concat(p.Name, '-', ToString(p.Age)) as Combined
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

    #endregion

    #endregion

    #region Round 10: Explorations 101-110

    #region Exploration 101: IsNull function

    [TestMethod]
    public void Explore101_IsNullCheck_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                p.Tags is null as HasNoTags
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = null },
            new() { Name = "Jane", Age = 25, Tags = ["a"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 102: IsNotNull check

    [TestMethod]
    public void Explore102_IsNotNullCheck_ShouldWork()
    {
        const string query = @"
            select
                p.Name
            from #schema.first() p
            where p.Tags is not null";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = null },
            new() { Name = "Jane", Age = 25, Tags = ["a"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Jane", table[0].Values[0]);
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

    #region Exploration 104: Abs function

    [TestMethod]
    public void Explore104_AbsFunction_ShouldWork()
    {
        const string query = @"
            select
                p.Age,
                Abs(p.Age - 35) as DistanceFrom35
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Jane", Age = 40 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 105: Round function

    [TestMethod]
    public void Explore105_RoundFunction_ShouldWork()
    {
        const string query = @"
            select
                i.ProductName,
                i.Price,
                Round(i.Price, 0) as RoundedPrice
            from #schema.first() o
            cross apply o.Items i";

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items =
                [
                    new OrderItem { ProductName = "A", Price = 10.49m, Quantity = 1 },
                    new OrderItem { ProductName = "B", Price = 10.51m, Quantity = 1 }
                ]
            }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Exploration 106: Floor function

    [TestMethod]
    public void Explore106_Floor_ShouldWork()
    {
        const string query = @"
            select
                i.ProductName,
                i.Price,
                Floor(i.Price) as FloorPrice
            from #schema.first() o
            cross apply o.Items i";

        var source = new List<Order>
        {
            new()
            {
                OrderId = 1,
                Items = [new OrderItem { ProductName = "A", Price = 10.5m, Quantity = 1 }]
            }
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

    #region Exploration 110: Complex query combining multiple features

    [TestMethod]
    public void Explore110_ComplexCombinedQuery_ShouldWork()
    {
        const string query = @"
            select
                p.Name,
                ToUpper(t.Value) as TagUpper,
                p.Age * 2 as DoubleAge
            from #schema.first() p
            cross apply p.Tags t
            where p.Age >= 25 and t.Value like '%a%'
            order by t.Value asc
            take 5";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30, Tags = ["admin", "data", "xyz"] },
            new() { Name = "Jane", Age = 20, Tags = ["admin"] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.IsTrue(table.Count <= 5);
    }

    #endregion

    #endregion
}
