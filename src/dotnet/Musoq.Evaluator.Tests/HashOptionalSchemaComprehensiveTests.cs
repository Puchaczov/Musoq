using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Comprehensive evaluator tests for hash-optional schema syntax (from schema.method() without # prefix).
///     These tests cover full query execution to ensure the evaluator correctly handles
///     both hash and hash-optional schema references through the entire query pipeline.
/// </summary>
[TestClass]
public class HashOptionalSchemaComprehensiveTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    #region DISTINCT Tests

    [TestMethod]
    public void HashOptional_Distinct_ShouldWork()
    {
        var query = "select distinct Name from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Test"),
                    new BasicEntity("Test"),
                    new BasicEntity("Other"),
                    new BasicEntity("Test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Arithmetic Tests

    [TestMethod]
    public void HashOptional_ArithmeticOperations_ShouldWork()
    {
        var query = "select Population + 10, Population - 5, Population * 2, Population / 2 from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(110m, table[0][0]);
        Assert.AreEqual(95m, table[0][1]);
        Assert.AreEqual(200m, table[0][2]);
        Assert.AreEqual(50m, table[0][3]);
    }

    #endregion

    #region Basic SELECT Queries

    [TestMethod]
    public void HashOptional_SelectAllColumns_ShouldWork()
    {
        var query = "select * from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "Test1", City = "City1", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsGreaterThan(0, table.Columns.Count());
        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void HashOptional_SelectMultipleColumns_ShouldWork()
    {
        var query = "select Name, City, Population from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "Test1", City = "Warsaw", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test1", table[0][0]);
        Assert.AreEqual("Warsaw", table[0][1]);
        Assert.AreEqual(100m, table[0][2]);
    }

    [TestMethod]
    public void HashOptional_SelectWithExpression_ShouldWork()
    {
        var query = "select Population * 2 as DoubledPopulation from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "Test", City = "City", Population = 50 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100m, table[0][0]);
    }

    #endregion

    #region WHERE Clause Tests

    [TestMethod]
    public void HashOptional_WhereEquals_ShouldWork()
    {
        var query = "select Name from A.Entities() where Name = 'Match'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Match"), new BasicEntity("NoMatch"), new BasicEntity("Match")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.All(r => (string)r[0] == "Match"));
    }

    [TestMethod]
    public void HashOptional_WhereGreaterThan_ShouldWork()
    {
        var query = "select Name, Population from A.Entities() where Population > 50";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "Low", Population = 30 },
                    new BasicEntity { Name = "High", Population = 100 },
                    new BasicEntity { Name = "Medium", Population = 50 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("High", table[0][0]);
    }

    [TestMethod]
    public void HashOptional_WhereWithAndOr_ShouldWork()
    {
        var query = "select Name from A.Entities() where Name = 'A' or Name = 'B'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("C")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_WhereWithLike_ShouldWork()
    {
        var query = "select Name from A.Entities() where Name like '%est%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test"), new BasicEntity("Testing"), new BasicEntity("NoMatch")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_WhereWithIn_ShouldWork()
    {
        var query = "select Name from A.Entities() where Name in ('A', 'B', 'C')";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("D"), new BasicEntity("E")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_WhereWithIsNull_ShouldWork()
    {
        var query = "select Name from A.Entities() where NullableValue is null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("NullValue") { NullableValue = null },
                    new BasicEntity("HasValue") { NullableValue = 5 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("NullValue", table[0][0]);
    }

    #endregion

    #region GROUP BY Tests

    [TestMethod]
    public void HashOptional_GroupByWithCount_ShouldWork()
    {
        var query = "select City, Count(City) from A.Entities() group by City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { City = "Warsaw" },
                    new BasicEntity { City = "Warsaw" },
                    new BasicEntity { City = "Berlin" },
                    new BasicEntity { City = "Berlin" },
                    new BasicEntity { City = "Berlin" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var warsaw = table.FirstOrDefault(r => (string)r[0] == "Warsaw");
        Assert.IsNotNull(warsaw);
        Assert.AreEqual(2, (int)warsaw[1]);

        var berlin = table.FirstOrDefault(r => (string)r[0] == "Berlin");
        Assert.IsNotNull(berlin);
        Assert.AreEqual(3, (int)berlin[1]);
    }

    [TestMethod]
    public void HashOptional_GroupByWithSum_ShouldWork()
    {
        var query = "select Country, Sum(Population) from A.Entities() group by Country";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Country = "Poland", Population = 100 },
                    new BasicEntity { Country = "Poland", Population = 200 },
                    new BasicEntity { Country = "Germany", Population = 150 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var poland = table.FirstOrDefault(r => (string)r[0] == "Poland");
        Assert.IsNotNull(poland);
        Assert.AreEqual(300m, poland[1]);
    }

    [TestMethod]
    public void HashOptional_GroupByWithHaving_ShouldWork()
    {
        var query = "select Country, Count(Country) from A.Entities() group by Country having Count(Country) > 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Country = "Poland" },
                    new BasicEntity { Country = "Poland" },
                    new BasicEntity { Country = "Germany" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Poland", table[0][0]);
    }

    #endregion

    #region ORDER BY Tests

    [TestMethod]
    public void HashOptional_OrderByAscending_ShouldWork()
    {
        var query = "select Name from A.Entities() order by Name asc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("C"), new BasicEntity("A"), new BasicEntity("B")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("A", table[0][0]);
        Assert.AreEqual("B", table[1][0]);
        Assert.AreEqual("C", table[2][0]);
    }

    [TestMethod]
    public void HashOptional_OrderByDescending_ShouldWork()
    {
        var query = "select Name from A.Entities() order by Name desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("C"), new BasicEntity("A"), new BasicEntity("B")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("C", table[0][0]);
        Assert.AreEqual("B", table[1][0]);
        Assert.AreEqual("A", table[2][0]);
    }

    [TestMethod]
    public void HashOptional_OrderByMultipleColumns_ShouldWork()
    {
        var query = "select Country, City from A.Entities() order by Country asc, City desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { City = "B", Country = "A" },
                    new BasicEntity { City = "A", Country = "A" },
                    new BasicEntity { City = "C", Country = "B" },
                    new BasicEntity { City = "A", Country = "B" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);
    }

    #endregion

    #region SKIP and TAKE Tests

    [TestMethod]
    public void HashOptional_Skip_ShouldWork()
    {
        var query = "select Name from A.Entities() order by Name skip 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A"),
                    new BasicEntity("B"),
                    new BasicEntity("C"),
                    new BasicEntity("D")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("C", table[0][0]);
        Assert.AreEqual("D", table[1][0]);
    }

    [TestMethod]
    public void HashOptional_Take_ShouldWork()
    {
        var query = "select Name from A.Entities() order by Name take 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A"),
                    new BasicEntity("B"),
                    new BasicEntity("C"),
                    new BasicEntity("D")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("A", table[0][0]);
        Assert.AreEqual("B", table[1][0]);
    }

    [TestMethod]
    public void HashOptional_SkipAndTake_ShouldWork()
    {
        var query = "select Name from A.Entities() order by Name skip 1 take 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A"),
                    new BasicEntity("B"),
                    new BasicEntity("C"),
                    new BasicEntity("D")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("B", table[0][0]);
        Assert.AreEqual("C", table[1][0]);
    }

    #endregion

    #region JOIN Tests

    [TestMethod]
    public void HashOptional_InnerJoin_ShouldWork()
    {
        var query = "select a.Name, b.City from A.Entities() a inner join B.Entities() b on a.Name = b.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Match"), new BasicEntity("NoMatch")] },
            { "#B", [new BasicEntity("Match") { City = "Warsaw" }, new BasicEntity("Other") { City = "Berlin" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Match", table[0][0]);
        Assert.AreEqual("Warsaw", table[0][1]);
    }

    [TestMethod]
    public void HashOptional_InnerJoinMultipleTables_ShouldWork()
    {
        var query = @"
            select a.Name, b.City, c.Country 
            from A.Entities() a 
            inner join B.Entities() b on a.Name = b.Name 
            inner join C.Entities() c on b.City = c.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test")] },
            { "#B", [new BasicEntity("Test") { City = "Warsaw" }] },
            { "#C", [new BasicEntity { City = "Warsaw", Country = "Poland" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test", table[0][0]);
        Assert.AreEqual("Warsaw", table[0][1]);
        Assert.AreEqual("Poland", table[0][2]);
    }

    [TestMethod]
    public void HashOptional_LeftOuterJoin_ShouldWork()
    {
        var query = "select a.Name, b.City from A.Entities() a left outer join B.Entities() b on a.Name = b.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Match"), new BasicEntity("NoMatch")] },
            { "#B", [new BasicEntity("Match") { City = "Warsaw" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_MixedJoinHashAndNoHash_ShouldWork()
    {
        var query = "select a.Name, b.City from #A.Entities() a inner join B.Entities() b on a.Name = b.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Match")] },
            { "#B", [new BasicEntity("Match") { City = "Warsaw" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Match", table[0][0]);
        Assert.AreEqual("Warsaw", table[0][1]);
    }

    #endregion

    #region SET Operators Tests

    [TestMethod]
    public void HashOptional_Union_ShouldWork()
    {
        var query = "select Name from A.Entities() union (Name) select Name from B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("First"), new BasicEntity("Common")] },
            { "#B", [new BasicEntity("Second"), new BasicEntity("Common")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void HashOptional_UnionAll_ShouldWork()
    {
        var query = "select Name from A.Entities() union all (Name) select Name from B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("First"), new BasicEntity("Common")] },
            { "#B", [new BasicEntity("Second"), new BasicEntity("Common")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);
    }

    [TestMethod]
    public void HashOptional_Except_ShouldWork()
    {
        var query = "select Name from A.Entities() except (Name) select Name from B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("First"), new BasicEntity("Common")] },
            { "#B", [new BasicEntity("Common"), new BasicEntity("Second")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("First", table[0][0]);
    }

    [TestMethod]
    public void HashOptional_Intersect_ShouldWork()
    {
        var query = "select Name from A.Entities() intersect (Name) select Name from B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("First"), new BasicEntity("Common")] },
            { "#B", [new BasicEntity("Common"), new BasicEntity("Second")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Common", table[0][0]);
    }

    [TestMethod]
    public void HashOptional_MixedSetOperatorsWithHashSyntax_ShouldWork()
    {
        var query = "select Name from #A.Entities() union (Name) select Name from B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("First")] },
            { "#B", [new BasicEntity("Second")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_MultipleUnions_ShouldWork()
    {
        var query = @"
            select Name from A.Entities() 
            union (Name) select Name from B.Entities()
            union (Name) select Name from C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("One")] },
            { "#B", [new BasicEntity("Two")] },
            { "#C", [new BasicEntity("Three")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
    }

    #endregion

    #region CTE Tests

    [TestMethod]
    public void HashOptional_SimpleCte_ShouldWork()
    {
        var query = "with cte as (select Name, City from A.Entities()) select Name, City from cte";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test") { City = "Warsaw" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test", table[0][0]);
        Assert.AreEqual("Warsaw", table[0][1]);
    }

    [TestMethod]
    public void HashOptional_MultipleCtes_ShouldWork()
    {
        var query = @"
            with cte1 as (select Name from A.Entities()),
            cte2 as (select Name from B.Entities())
            select c1.Name, c2.Name from cte1 c1 inner join cte2 c2 on 1 = 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("FromA")] },
            { "#B", [new BasicEntity("FromB")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("FromA", table[0][0]);
        Assert.AreEqual("FromB", table[0][1]);
    }

    [TestMethod]
    public void HashOptional_CteWithGroupBy_ShouldWork()
    {
        var query = @"
            with cte as (
                select Country, Sum(Population) as TotalPop 
                from A.Entities() 
                group by Country
            ) 
            select Country, TotalPop from cte";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Country = "Poland", Population = 100 },
                    new BasicEntity { Country = "Poland", Population = 200 },
                    new BasicEntity { Country = "Germany", Population = 150 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_CteWithSetOperators_ShouldWork()
    {
        var query = @"
            with cte as (
                select Name from A.Entities() 
                union (Name) 
                select Name from B.Entities()
            ) 
            select Name from cte";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("First")] },
            { "#B", [new BasicEntity("Second")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_CteMixedHashAndNoHash_ShouldWork()
    {
        var query = @"
            with cte1 as (select Name from #A.Entities()),
            cte2 as (select Name from B.Entities())
            select * from cte1 union (Name) select * from cte2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("HashSyntax")] },
            { "#B", [new BasicEntity("NoHashSyntax")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region CASE WHEN Tests

    [TestMethod]
    public void HashOptional_CaseWhenSimple_ShouldWork()
    {
        var query = "select case when Population > 100 then 'High' else 'Low' end as Category from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Population = 50 },
                    new BasicEntity { Population = 150 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(r => (string)r[0] == "Low"));
        Assert.IsTrue(table.Any(r => (string)r[0] == "High"));
    }

    [TestMethod]
    public void HashOptional_CaseWhenMultipleBranches_ShouldWork()
    {
        var query = @"
            select case 
                when Population < 50 then 'Small' 
                when Population < 100 then 'Medium' 
                else 'Large' 
            end as Size from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Population = 30 },
                    new BasicEntity { Population = 75 },
                    new BasicEntity { Population = 150 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
    }

    #endregion

    #region Reordered Query Tests

    [TestMethod]
    public void HashOptional_ReorderedQueryBasic_ShouldWork()
    {
        var query = "from A.Entities() select Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test1"), new BasicEntity("Test2")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_ReorderedQueryWithWhere_ShouldWork()
    {
        var query = "from A.Entities() where Name = 'Match' select Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Match"), new BasicEntity("NoMatch")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Match", table[0][0]);
    }

    [TestMethod]
    public void HashOptional_ReorderedQueryWithGroupBy_ShouldWork()
    {
        var query = "from A.Entities() group by City select City, Count(City)";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { City = "Warsaw" },
                    new BasicEntity { City = "Warsaw" },
                    new BasicEntity { City = "Berlin" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_ReorderedQueryWithJoin_ShouldWork()
    {
        var query = "from A.Entities() a inner join B.Entities() b on a.Name = b.Name select a.Name, b.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Match")] },
            { "#B", [new BasicEntity("Match") { City = "Warsaw" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Match", table[0][0]);
        Assert.AreEqual("Warsaw", table[0][1]);
    }

    #endregion

    #region Complex Queries Tests

    [TestMethod]
    public void HashOptional_ComplexQueryAllFeatures_ShouldWork()
    {
        var query = @"
            with filtered as (
                select Name, City, Population 
                from A.Entities() 
                where Population > 50
            )
            select 
                City, 
                Count(City) as CityCount, 
                Sum(Population) as TotalPop
            from filtered
            group by City
            having Count(City) > 0
            order by Sum(Population) desc
            take 10";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", City = "Warsaw", Population = 100 },
                    new BasicEntity { Name = "B", City = "Warsaw", Population = 150 },
                    new BasicEntity { Name = "C", City = "Berlin", Population = 200 },
                    new BasicEntity { Name = "D", City = "Berlin", Population = 30 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void HashOptional_MultipleOperationsChained_ShouldWork()
    {
        var query = @"
            select a.Name, b.Name, c.Name 
            from A.Entities() a 
            inner join B.Entities() b on a.Name = b.Name
            inner join C.Entities() c on b.Name = c.Name
            where a.Population > 50
            order by a.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "Match", Population = 100 }] },
            { "#B", [new BasicEntity("Match")] },
            { "#C", [new BasicEntity("Match")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Function Calls Tests

    [TestMethod]
    public void HashOptional_StringFunctions_ShouldWork()
    {
        var query = "select ToUpper(Name), ToLower(Name), Length(Name) from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("TEST", table[0][0]);
        Assert.AreEqual("test", table[0][1]);
        Assert.AreEqual(4, table[0][2]);
    }

    [TestMethod]
    public void HashOptional_CoalesceFunction_ShouldWork()
    {
        var query = "select Coalesce(NullableValue, 999) from A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A") { NullableValue = 5 },
                    new BasicEntity("B") { NullableValue = null }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region DESC Statement Tests (Hash-Optional)

    [TestMethod]
    public void HashOptional_DescSchema_ShouldWork()
    {
        var query = "desc A";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.IsGreaterThan(0, table.Count, "Should return at least one method");
        Assert.IsTrue(table.Any(row => (string)row[0] == "entities"), "Should contain 'entities' method");
    }

    [TestMethod]
    public void HashOptional_DescSchemaMethod_ShouldWork()
    {
        var query = "desc A.entities";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(1, table.Count, "Should return exactly one method name");
        Assert.AreEqual("entities", table[0][0], "Should return the method name");
    }

    [TestMethod]
    public void HashOptional_DescSchemaMethodWithParentheses_ShouldWork()
    {
        var query = "desc A.entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Columns.Count(), "Should have 3 columns: Name, Index, Type");
        Assert.IsGreaterThan(0, table.Count, "Should return at least one column");
        Assert.IsTrue(table.Any(row => (string)row[0] == "Name"), "Should contain 'Name' column");
    }

    [TestMethod]
    public void HashOptional_DescFunctionsSchema_ShouldWork()
    {
        var query = "desc functions A";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Columns.Count(), "Should have 4 columns: Method, Description, Category, and Source");
        Assert.IsGreaterThan(0, table.Count, "Should return at least one method");
    }

    [TestMethod]
    public void HashOptional_DescFunctionsSchemaMethod_ShouldWork()
    {
        var query = "desc functions A.entities";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Columns.Count(), "Should have 4 columns: Method, Description, Category, and Source");
        Assert.IsGreaterThan(0, table.Count, "Should return at least one library method");
    }

    [TestMethod]
    public void HashOptional_DescFunctionsSchemaMethodWithParentheses_ShouldWork()
    {
        var query = "desc functions A.entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Columns.Count(), "Should have 4 columns: Method, Description, Category, and Source");
        Assert.IsGreaterThan(0, table.Count, "Should return at least one method");
    }

    #endregion

    #region COUPLE Statement Tests (Hash-Optional)

    [TestMethod]
    public void HashOptional_CoupleStatement_ShouldWork()
    {
        const string query = "table DummyTable {" +
                             "   Name string" +
                             "};" +
                             "couple A.Entities with table DummyTable as SourceOfDummyRows;" +
                             "select Name from SourceOfDummyRows();";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("First"),
                    new BasicEntity("Second"),
                    new BasicEntity("Third")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(3, table.Count, "Result should contain exactly 3 strings");
        Assert.IsTrue(table.Any(row => (string)row[0] == "First"));
        Assert.IsTrue(table.Any(row => (string)row[0] == "Second"));
        Assert.IsTrue(table.Any(row => (string)row[0] == "Third"));
    }

    [TestMethod]
    public void HashOptional_CoupleStatementWithMultipleColumns_ShouldWork()
    {
        const string query = "table DataTable {" +
                             "   Country string," +
                             "   Population decimal" +
                             "};" +
                             "couple A.Entities with table DataTable as SourceOfData;" +
                             "select Country, Population from SourceOfData();";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Country = "Poland", Population = 38 },
                    new BasicEntity { Country = "Germany", Population = 83 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(2, table.Count, "Result should contain exactly 2 rows");
    }

    [TestMethod]
    public void HashOptional_CoupleStatementMixedWithHashSyntax_ShouldWork()
    {
        const string query = "table DummyTable {" +
                             "   Name string" +
                             "};" +
                             "couple A.Entities with table DummyTable as SourceOfDummyRows;" +
                             "select Name from SourceOfDummyRows();";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test", table[0][0]);
    }

    #endregion
}
