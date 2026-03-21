using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Exploratory tests: Cross Apply basics (Explorations 1-20).
/// </summary>
[TestClass]
public class Exploratory_CrossApplyBasicsTests : ExploratoryEvaluatorTestsBase
{
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
}
