using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Exploratory tests: Functions and fix verifications (Explorations 91-110+).
/// </summary>
[TestClass]
public class Exploratory_FunctionsAndFixesTests : ExploratoryEvaluatorTestsBase
{
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
        Assert.IsLessThanOrEqualTo(5, table.Count);
    }

    #endregion

    #region Fix Verification: Coalesce/IfNull with null literals

    [TestMethod]
    public void WhenCoalesceWithNullAndString_ShouldReturnFallback()
    {
        const string query = @"
            select Coalesce(null, 'fallback') as Result
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("fallback", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenIfNullWithNullAndString_ShouldReturnDefault()
    {
        const string query = @"
            select IfNull(null, 'fallback') as Result
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("fallback", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenIfNullWithNullAndInteger_ShouldReturnInteger()
    {
        const string query = @"
            select IfNull(null, 42) as V
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 30 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenCoalesceWithNullAndColumnValue_ShouldReturnColumnValue()
    {
        const string query = @"
            select Coalesce(null, p.Name) as Result
            from #schema.first() p";

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 25 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Alice", table[0].Values[0]);
    }

    #endregion

    #region Fix Verification: String comparison operators

    [TestMethod]
    public void WhenStringGreaterThanOrEqual_ShouldCompareCorrectly()
    {
        const string query = @"
            select p.Name
            from #schema.first() p
            where p.Name >= 'Charlie'
            order by p.Name asc";

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Charlie", Age = 25 },
            new() { Name = "Eve", Age = 20 },
            new() { Name = "Bob", Age = 35 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Charlie", table[0].Values[0]);
        Assert.AreEqual("Eve", table[1].Values[0]);
    }

    [TestMethod]
    public void WhenStringLessThan_ShouldCompareCorrectly()
    {
        const string query = @"
            select p.Name
            from #schema.first() p
            where p.Name < 'Charlie'
            order by p.Name asc";

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Charlie", Age = 25 },
            new() { Name = "Eve", Age = 20 },
            new() { Name = "Bob", Age = 35 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Alice", table[0].Values[0]);
        Assert.AreEqual("Bob", table[1].Values[0]);
    }

    [TestMethod]
    public void WhenStringGreaterThan_ShouldCompareCorrectly()
    {
        const string query = @"
            select p.Name
            from #schema.first() p
            where p.Name > 'Charlie'";

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Charlie", Age = 25 },
            new() { Name = "Eve", Age = 20 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Eve", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenStringLessThanOrEqual_ShouldCompareCorrectly()
    {
        const string query = @"
            select p.Name
            from #schema.first() p
            where p.Name <= 'Charlie'
            order by p.Name asc";

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Charlie", Age = 25 },
            new() { Name = "Eve", Age = 20 },
            new() { Name = "Bob", Age = 35 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Alice", table[0].Values[0]);
        Assert.AreEqual("Bob", table[1].Values[0]);
        Assert.AreEqual("Charlie", table[2].Values[0]);
    }

    #endregion

    #region Fix Verification: GROUP BY complex expressions

    [TestMethod]
    public void WhenGroupByArithmeticExpression_ShouldWork()
    {
        const string query = @"
            select
                p.Age / 10 * 10 as AgeDecade,
                Count(p.Name) as PersonCount
            from #schema.first() p
            group by p.Age / 10 * 10
            order by p.Age / 10 * 10 asc";

        var source = new List<Person>
        {
            new() { Name = "John", Age = 31 },
            new() { Name = "Jane", Age = 35 },
            new() { Name = "Bob", Age = 42 },
            new() { Name = "Sue", Age = 48 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(30, table[0].Values[0]);
        Assert.AreEqual(2, table[0].Values[1]);
        Assert.AreEqual(40, table[1].Values[0]);
        Assert.AreEqual(2, table[1].Values[1]);
    }

    [TestMethod]
    public void WhenGroupByAddExpression_ShouldWork()
    {
        const string query = @"
            select
                p.Age + 100 as AgeShifted,
                Count(p.Name) as PersonCount
            from #schema.first() p
            group by p.Age + 100";

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

    #region Fix Verification: Simple CASE form

    [TestMethod]
    public void WhenSimpleCaseWithIntegerValues_ShouldWork()
    {
        const string query = @"
            select
                case p.Age
                    when 30 then 'thirty'
                    when 25 then 'twenty-five'
                    else 'other'
                end as AgeLabel
            from #schema.first() p
            order by p.Name asc";

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Bob", Age = 25 },
            new() { Name = "Charlie", Age = 40 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("thirty", table[0].Values[0]);
        Assert.AreEqual("twenty-five", table[1].Values[0]);
        Assert.AreEqual("other", table[2].Values[0]);
    }

    [TestMethod]
    public void WhenSimpleCaseWithStringValues_ShouldWork()
    {
        const string query = @"
            select
                case p.Name
                    when 'Alice' then 1
                    when 'Bob' then 2
                    else 0
                end as NameCode
            from #schema.first() p
            order by p.Name asc";

        var source = new List<Person>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Bob", Age = 25 },
            new() { Name = "Charlie", Age = 40 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsNotNull(table);
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(1, table[0].Values[0]);
        Assert.AreEqual(2, table[1].Values[0]);
        Assert.AreEqual(0, table[2].Values[0]);
    }

    #endregion
}
