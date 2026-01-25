using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Integration tests designed to hit uncovered branches in the evaluator.
///     These tests execute complete SQL queries through the entire pipeline
///     to exercise code paths in visitors, emitters, and code generation.
/// </summary>
[TestClass]
public class BranchCoverageIntegrationTests : BasicEntityTestBase
{
    #region NULL Handling Tests

    [TestMethod]
    public void Query_IsNullInWhere_ShouldWork()
    {
        var query = "select Name from #A.Entities() where NullableValue is null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("HasValue") { NullableValue = 5 },
                    new BasicEntity("NoValue") { NullableValue = null }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("NoValue", table[0][0]);
    }

    #endregion

    #region Chained Set Operations

    [TestMethod]
    public void Query_ChainedExcept_ShouldWork()
    {
        var query = @"
            select Name from #A.Entities()
            except (Name)
            select Name from #B.Entities()
            except (Name)
            select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("C"), new BasicEntity("D")] },
            { "#B", [new BasicEntity("B")] },
            { "#C", [new BasicEntity("C")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Complex CASE Expressions

    [TestMethod]
    public void Query_NestedCaseWhen_ShouldWork()
    {
        var query = @"
            select 
                case 
                    when Population > 100 then 
                        case 
                            when City = 'NYC' then 'Big NYC' 
                            else 'Big Other' 
                        end
                    else 'Small'
                end as Size
            from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", City = "NYC", Population = 200 },
                    new BasicEntity { Name = "B", City = "LA", Population = 200 },
                    new BasicEntity { Name = "C", City = "NYC", Population = 50 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
    }

    #endregion

    #region Complex WHERE Clause Tests

    [TestMethod]
    public void Query_ComplexWhereWithAndOr_ShouldWork()
    {
        var query = @"
            select Name 
            from #A.Entities() 
            where (Name = 'A' or Name = 'B') and Population > 50";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", Population = 100 },
                    new BasicEntity { Name = "B", Population = 30 },
                    new BasicEntity { Name = "C", Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A", table[0][0]);
    }

    [TestMethod]
    public void Query_WhereWithNotEquals_ShouldWork()
    {
        var query = "select Name from #A.Entities() where Name <> 'A'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A" }, new BasicEntity { Name = "B" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("B", table[0][0]);
    }

    [TestMethod]
    public void Query_WhereWithNotIn_ShouldWork()
    {
        var query = "select Name from #A.Entities() where Name not in ('A', 'B')";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("C")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("C", table[0][0]);
    }

    [TestMethod]
    public void Query_WhereWithNotLike_ShouldWork()
    {
        var query = "select Name from #A.Entities() where Name not like 'Test%'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Test1"), new BasicEntity("Other"), new BasicEntity("Test2")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Other", table[0][0]);
    }

    [TestMethod]
    public void Query_WhereWithBetween_ShouldWork()
    {
        var query = "select Name from #A.Entities() where Population >= 100 and Population <= 200";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", Population = 50 },
                    new BasicEntity { Name = "B", Population = 150 },
                    new BasicEntity { Name = "C", Population = 250 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("B", table[0][0]);
    }

    [TestMethod]
    public void Query_WhereWithIsNotNull_ShouldWork()
    {
        var query = "select Name from #A.Entities() where NullableValue is not null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("HasValue") { NullableValue = 5 },
                    new BasicEntity("NoValue") { NullableValue = null }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("HasValue", table[0][0]);
    }

    #endregion

    #region CASE WHEN Tests

    [TestMethod]
    public void Query_CaseWhenSimple_ShouldWork()
    {
        var query = @"
            select Name,
                case when Population > 100 then 'High' else 'Low' end as Category
            from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", Population = 50 },
                    new BasicEntity { Name = "B", Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Query_CaseWhenMultipleConditions_ShouldWork()
    {
        var query = @"
            select Name,
                case 
                    when Population > 200 then 'High'
                    when Population > 100 then 'Medium'
                    else 'Low'
                end as Category
            from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", Population = 50 },
                    new BasicEntity { Name = "B", Population = 150 },
                    new BasicEntity { Name = "C", Population = 300 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void Query_CaseWhenInWhere_ShouldWork()
    {
        var query = @"
            select Name
            from #A.Entities()
            where case when Population > 100 then 1 else 0 end = 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", Population = 50 },
                    new BasicEntity { Name = "B", Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("B", table[0][0]);
    }

    [TestMethod]
    public void Query_CaseWhenWithNullHandling_ShouldWork()
    {
        var query = @"
            select Name,
                case 
                    when NullableValue is null then 'No Value'
                    when NullableValue > 5 then 'High'
                    else 'Low'
                end as Category
            from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A") { NullableValue = null },
                    new BasicEntity("B") { NullableValue = 10 },
                    new BasicEntity("C") { NullableValue = 3 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
    }

    #endregion

    #region Aggregate and GROUP BY Tests

    [TestMethod]
    public void Query_GroupByWithMultipleAggregates_ShouldWork()
    {
        var query = @"
            select City, 
                Count(City) as Cnt,
                Sum(Population) as TotalPop,
                Avg(Population) as AvgPop,
                Min(Population) as MinPop,
                Max(Population) as MaxPop
            from #A.Entities()
            group by City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", City = "NYC", Population = 100 },
                    new BasicEntity { Name = "B", City = "NYC", Population = 200 },
                    new BasicEntity { Name = "C", City = "LA", Population = 50 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Query_GroupByWithHaving_ShouldWork()
    {
        var query = @"
            select City, Count(City) as Cnt
            from #A.Entities()
            group by City
            having Count(City) > 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", City = "NYC" },
                    new BasicEntity { Name = "B", City = "NYC" },
                    new BasicEntity { Name = "C", City = "LA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("NYC", table[0][0]);
    }

    [TestMethod]
    public void Query_GroupByMultipleColumns_ShouldWork()
    {
        var query = @"
            select City, Country, Count(City) as Cnt
            from #A.Entities()
            group by City, Country";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", City = "NYC", Country = "USA" },
                    new BasicEntity { Name = "B", City = "NYC", Country = "USA" },
                    new BasicEntity { Name = "C", City = "LA", Country = "USA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region JOIN Tests

    [TestMethod]
    public void Query_LeftJoin_ShouldWork()
    {
        var query = @"
            select a.Name, b.Name 
            from #A.Entities() a 
            left outer join #B.Entities() b on a.City = b.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [new BasicEntity { Name = "PersonA", City = "NYC" }, new BasicEntity { Name = "PersonC", City = "LA" }]
            },
            { "#B", [new BasicEntity { Name = "PersonB", City = "NYC" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Query_RightJoin_ShouldWork()
    {
        var query = @"
            select a.Name, b.Name 
            from #A.Entities() a 
            right outer join #B.Entities() b on a.City = b.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "PersonA", City = "NYC" }] },
            {
                "#B",
                [new BasicEntity { Name = "PersonB", City = "NYC" }, new BasicEntity { Name = "PersonC", City = "LA" }]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Query_CrossJoin_ShouldWork()
    {
        var query = @"
            select a.Name, b.Name 
            from #A.Entities() a
            inner join #B.Entities() b on 1 = 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1" }, new BasicEntity { Name = "A2" }] },
            { "#B", [new BasicEntity { Name = "B1" }, new BasicEntity { Name = "B2" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(4, table.Count);
    }

    [TestMethod]
    public void Query_MultipleJoins_ShouldWork()
    {
        var query = @"
            select a.Name, b.Name, c.Name 
            from #A.Entities() a 
            inner join #B.Entities() b on a.City = b.City
            inner join #C.Entities() c on b.Country = c.Country";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A", City = "NYC", Country = "USA" }] },
            { "#B", [new BasicEntity { Name = "B", City = "NYC", Country = "USA" }] },
            { "#C", [new BasicEntity { Name = "C", City = "LA", Country = "USA" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void Query_SelfJoin_ShouldWork()
    {
        var query = @"
            select a.Name, b.Name 
            from #A.Entities() a 
            inner join #A.Entities() b on a.City = b.City
            where a.Name <> b.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [new BasicEntity { Name = "PersonA", City = "NYC" }, new BasicEntity { Name = "PersonB", City = "NYC" }]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region UNION/EXCEPT/INTERSECT Tests

    [TestMethod]
    public void Query_UnionAll_ShouldWork()
    {
        var query = @"
            select Name from #A.Entities()
            union all (Name)
            select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B")] },
            { "#B", [new BasicEntity("B"), new BasicEntity("C")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(4, table.Count);
    }

    [TestMethod]
    public void Query_Union_ShouldWork()
    {
        var query = @"
            select Name from #A.Entities()
            union (Name)
            select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B")] },
            { "#B", [new BasicEntity("B"), new BasicEntity("C")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void Query_Except_ShouldWork()
    {
        var query = @"
            select Name from #A.Entities()
            except (Name)
            select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("C")] },
            { "#B", [new BasicEntity("B")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Query_Intersect_ShouldWork()
    {
        var query = @"
            select Name from #A.Entities()
            intersect (Name)
            select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("C")] },
            { "#B", [new BasicEntity("B"), new BasicEntity("C"), new BasicEntity("D")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region CTE Tests

    [TestMethod]
    public void Query_WithCTE_ShouldWork()
    {
        var query = @"
            with filtered as (
                select Name, Population from #A.Entities() where Population > 100
            )
            select Name from filtered";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "A", Population = 50 }, new BasicEntity { Name = "B", Population = 200 },
                    new BasicEntity { Name = "C", Population = 150 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Query_MultipleCTEs_ShouldWork()
    {
        var query = @"
            with high as (
                select Name, Population from #A.Entities() where Population > 150
            ),
            medium as (
                select Name, Population from #A.Entities() where Population > 50 and Population <= 150
            )
            select Name from high
            union all (Name)
            select Name from medium";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "A", Population = 50 }, new BasicEntity { Name = "B", Population = 200 },
                    new BasicEntity { Name = "C", Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region DISTINCT Tests

    [TestMethod]
    public void Query_DistinctMultipleColumns_ShouldWork()
    {
        var query = "select distinct City, Country from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", City = "NYC", Country = "USA" },
                    new BasicEntity { Name = "B", City = "NYC", Country = "USA" },
                    new BasicEntity { Name = "C", City = "LA", Country = "USA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Query_DistinctWithOrderBy_ShouldWork()
    {
        var query = "select distinct City from #A.Entities() order by City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A", City = "NYC" },
                    new BasicEntity { Name = "B", City = "LA" },
                    new BasicEntity { Name = "C", City = "NYC" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Complex Expression Tests

    [TestMethod]
    public void Query_NestedArithmetic_ShouldWork()
    {
        var query = "select ((Population * 2) + 10) / 5 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void Query_StringFunctions_ShouldWork()
    {
        var query = "select ToUpperInvariant(Name), ToLowerInvariant(City), Length(Name) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "Test", City = "NYC" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("TEST", table[0][0]);
        Assert.AreEqual("nyc", table[0][1]);
        Assert.AreEqual(4, Convert.ToInt32(table[0][2]));
    }

    [TestMethod]
    public void Query_NullCoalesce_ShouldWork()
    {
        var query = "select Coalesce(NullableValue, -1) from #A.Entities()";
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
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Order By Tests

    [TestMethod]
    public void Query_OrderByMultipleColumns_ShouldWork()
    {
        var query = "select Name, City from #A.Entities() order by City asc, Name desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "B", City = "NYC" },
                    new BasicEntity { Name = "A", City = "NYC" },
                    new BasicEntity { Name = "C", City = "LA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void Query_OrderByWithNulls_ShouldWork()
    {
        var query = "select Name, NullableValue from #A.Entities() order by NullableValue";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A") { NullableValue = 10 },
                    new BasicEntity("B") { NullableValue = null },
                    new BasicEntity("C") { NullableValue = 5 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void Query_OrderByExpression_ShouldWork()
    {
        var query = "select Name, Population from #A.Entities() order by Population";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "B", Population = 200 },
                    new BasicEntity { Name = "A", Population = 100 },
                    new BasicEntity { Name = "C", Population = 50 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
    }

    #endregion

    #region Type Conversion Tests

    [TestMethod]
    public void Query_ImplicitTypeConversion_ShouldWork()
    {
        var query = "select Population + 1.5 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void Query_ToString_ShouldWork()
    {
        var query = "select ToString(Population) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("100", table[0][0]);
    }

    [TestMethod]
    public void Query_ToInt32_ShouldWork()
    {
        var query = "select ToInt32('123') from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(123, table[0][0]);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Query_EmptyResult_ShouldWork()
    {
        var query = "select Name from #A.Entities() where 1 = 0";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void Query_NoSourceRows_ShouldWork()
    {
        var query = "select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", Array.Empty<BasicEntity>() }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void Query_VeryLongInList_ShouldWork()
    {
        var inList = string.Join(", ", Enumerable.Range(1, 50).Select(i => $"'{i}'"));
        var query = $"select Name from #A.Entities() where Name in ({inList})";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("25"), new BasicEntity("100")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("25", table[0][0]);
    }

    #endregion

    #region Boolean and Comparison Tests

    [TestMethod]
    public void Query_BooleanExpression_ShouldWork()
    {
        var query = "select Population > 100 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [new BasicEntity { Name = "A", Population = 50 }, new BasicEntity { Name = "B", Population = 200 }]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Query_AllComparisonOperators_ShouldWork()
    {
        var query = @"
            select 
                Population > 100,
                Population >= 100,
                Population < 100,
                Population <= 100,
                Population = 100,
                Population <> 100
            from #A.Entities()
            where Population = 100";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Skip/Take Tests

    [TestMethod]
    public void Query_SkipOnly_ShouldWork()
    {
        var query = "select Name from #A.Entities() order by Name skip 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("C"), new BasicEntity("D")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Query_TakeOnly_ShouldWork()
    {
        var query = "select Name from #A.Entities() order by Name take 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("C"), new BasicEntity("D")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Query_SkipMoreThanRows_ShouldReturnEmpty()
    {
        var query = "select Name from #A.Entities() order by Name skip 100";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void Query_SkipAndTake_ShouldWork()
    {
        var query = "select Name from #A.Entities() order by Name skip 1 take 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("B"), new BasicEntity("C"), new BasicEntity("D")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Complex Multi-Table Queries

    [TestMethod]
    public void Query_JoinWithGroupBy_ShouldWork()
    {
        var query = @"
            select a.City, a.Count(a.City) as Cnt
            from #A.Entities() a
            inner join #B.Entities() b on a.City = b.City
            group by a.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", City = "NYC" }, new BasicEntity { Name = "A2", City = "NYC" }] },
            { "#B", [new BasicEntity { Name = "B1", City = "NYC" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void Query_UnionWithOrderBy_ShouldWork()
    {
        var query = @"
            select Name from #A.Entities()
            union all (Name)
            select Name from #B.Entities()
            order by Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("C"), new BasicEntity("A")] },
            { "#B", [new BasicEntity("B"), new BasicEntity("D")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(4, table.Count);
    }

    [TestMethod]
    public void Query_CTEWithJoin_ShouldWork()
    {
        var query = @"
            with filtered as (
                select Name, City from #A.Entities() where Population > 50
            )
            select f.Name, b.Name
            from filtered f
            inner join #B.Entities() b on f.City = b.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "A", City = "NYC", Population = 100 },
                    new BasicEntity { Name = "C", City = "NYC", Population = 30 }
                ]
            },
            { "#B", [new BasicEntity { Name = "B", City = "NYC" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Additional Math and String Operations

    [TestMethod]
    public void Query_ModuloOperator_ShouldWork()
    {
        var query = "select Population % 3 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "A", Population = 10 }, new BasicEntity { Name = "B", Population = 9 },
                    new BasicEntity { Name = "C", Population = 8 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void Query_StringConcat_ShouldWork()
    {
        var query = "select Name + ' - ' + City from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "Test", City = "NYC" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test - NYC", table[0][0]);
    }

    [TestMethod]
    public void Query_SubstringFunction_ShouldWork()
    {
        var query = "select Substring(Name, 0, 2) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Testing")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Te", table[0][0]);
    }

    [TestMethod]
    public void Query_NegativeNumber_ShouldWork()
    {
        var query = "select -Population from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A", Population = 100 }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(-100m, table[0][0]);
    }

    #endregion
}
