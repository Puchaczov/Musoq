using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Exploratory tests: Advanced queries and joins (Explorations 21-50).
/// </summary>
[TestClass]
public class Exploratory_QueriesAndJoinsTests : ExploratoryEvaluatorTestsBase
{
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
    public void Explore29_CrossApply_GroupByMultipleColumns_ShouldWork()
    {
        const string query = @"
            select p.Name, s.Value, Count(1) as Cnt
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
        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row => row.Values[0].ToString() == "John" && (int)row.Values[1] == 10 && (int)row.Values[2] == 2));
        Assert.IsTrue(table.Any(row => row.Values[0].ToString() == "John" && (int)row.Values[1] == 20 && (int)row.Values[2] == 2));
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
}
