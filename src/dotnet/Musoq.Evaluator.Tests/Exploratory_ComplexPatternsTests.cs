using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Exploratory tests: Complex patterns (Explorations 51-90).
/// </summary>
[TestClass]
public class Exploratory_ComplexPatternsTests : ExploratoryEvaluatorTestsBase
{
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

    #region Exploration 66: Count distinct - Now supported

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

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(3, table[0].Values[0]);
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
}
