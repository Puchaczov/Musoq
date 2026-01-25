using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Comprehensive tests for reordered SQL syntax (FROM-first) with CTEs (Common Table Expressions).
///     These tests verify that the reordered syntax works correctly in various complex scenarios
///     including nested CTEs, set operators, joins, and mixed syntax usage.
/// </summary>
[TestClass]
public class ReorderedSyntaxCteTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    #region Category 1: CTE with Reordered Inner Query

    [TestMethod]
    public void CteWithReorderedInnerQuery_BasicSelect_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() select City, Country
            )
            select City, Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("City", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Country", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "WARSAW" &&
            (string)row.Values[1] == "POLAND"));
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "BERLIN" &&
            (string)row.Values[1] == "GERMANY"));
    }

    [TestMethod]
    public void CteWithReorderedInnerQuery_WithWhere_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() where Country = 'POLAND' select City, Country
            )
            select City, Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.All(row => (string)row.Values[1] == "POLAND"));
    }

    [TestMethod]
    public void CteWithReorderedInnerQuery_WithGroupBy_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() group by Country select Country, Sum(Population) as TotalPop
            )
            select Country, TotalPop from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "POLAND" &&
            (decimal)row.Values[1] == 900m));
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "GERMANY" &&
            (decimal)row.Values[1] == 250m));
    }

    [TestMethod]
    public void CteWithReorderedInnerQuery_WithJoin_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() a 
                inner join #B.Entities() b on a.Country = b.Country 
                select a.City as City, a.Country as Country, b.Population as OtherPop
            )
            select City, Country, OtherPop from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            },
            {
                "#B", [
                    new BasicEntity("KRAKOW", "POLAND", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", table[0].Values[0]);
        Assert.AreEqual("POLAND", table[0].Values[1]);
        Assert.AreEqual(300m, table[0].Values[2]);
    }

    [TestMethod]
    public void CteWithReorderedInnerQuery_WithOrderBy_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() select City, Population order by Population desc
            )
            select City, Population from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        var cities = table.Select(row => (string)row.Values[0]).ToList();
        CollectionAssert.Contains(cities, "WARSAW");
        CollectionAssert.Contains(cities, "CZESTOCHOWA");
        CollectionAssert.Contains(cities, "KATOWICE");
    }

    [TestMethod]
    public void CteWithReorderedInnerQuery_WithSkipTake_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() select City, Population order by Population desc skip 1 take 1
            )
            select City, Population from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        var city = (string)table[0].Values[0];
        Assert.IsTrue(city == "WARSAW" || city == "CZESTOCHOWA" || city == "KATOWICE",
            "Result should be one of the input values");
    }

    #endregion

    #region Category 2: CTE with Reordered Outer Query

    [TestMethod]
    public void CteWithReorderedOuterQuery_BasicSelect_ShouldWork()
    {
        var query = @"
            with cte as (
                select City, Country from #A.Entities()
            )
            from cte select City, Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "WARSAW" &&
            (string)row.Values[1] == "POLAND"));
    }

    [TestMethod]
    public void CteWithReorderedOuterQuery_WithWhere_ShouldWork()
    {
        var query = @"
            with cte as (
                select City, Country, Population from #A.Entities()
            )
            from cte where Population > 300 select City, Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "WARSAW"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "CZESTOCHOWA"));
    }

    [TestMethod]
    public void CteWithReorderedOuterQuery_WithGroupBy_ShouldWork()
    {
        var query = @"
            with cte as (
                select City, Country, Population from #A.Entities()
            )
            from cte group by Country select Country, Sum(Population) as TotalPop";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "POLAND" &&
            (decimal)row.Values[1] == 900m));
    }

    [TestMethod]
    public void CteWithReorderedOuterQuery_WithOrderBy_ShouldWork()
    {
        var query = @"
            with cte as (
                select City, Population from #A.Entities()
            )
            from cte select City, Population order by Population asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        var cities = table.Select(row => (string)row.Values[0]).ToList();
        CollectionAssert.Contains(cities, "KATOWICE");
        CollectionAssert.Contains(cities, "CZESTOCHOWA");
        CollectionAssert.Contains(cities, "WARSAW");
    }

    #endregion

    #region Category 3: Both CTE and Outer Query Reordered

    [TestMethod]
    public void BothCteAndOuterQueryReordered_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() where Country = 'POLAND' select City, Country, Population
            )
            from cte where Population > 300 select City, Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 600)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "WARSAW"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "CZESTOCHOWA"));
    }

    [TestMethod]
    public void MultipleCtes_AllReordered_ShouldWork()
    {
        var query = @"
            with 
                cte1 as (
                    from #A.Entities() where Country = 'POLAND' select City, Country, Population
                ),
                cte2 as (
                    from #B.Entities() where Country = 'GERMANY' select City, Country, Population
                )
            from cte1 select City, Country
            union (City, Country)
            from cte2 select City, Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            },
            {
                "#B", [
                    new BasicEntity("MUNICH", "GERMANY", 350),
                    new BasicEntity("KRAKOW", "POLAND", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "WARSAW" &&
            (string)row.Values[1] == "POLAND"));
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "MUNICH" &&
            (string)row.Values[1] == "GERMANY"));
    }

    [TestMethod]
    public void MultipleCtes_ReorderedWithGroupBy_ShouldWork()
    {
        var query = @"
            with 
                cte1 as (
                    from #A.Entities() group by Country select Country, Sum(Population) as TotalPop
                ),
                cte2 as (
                    from cte1 where TotalPop > 500 select Country, TotalPop
                )
            from cte2 select Country, TotalPop";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("POLAND", table[0].Values[0]);
        Assert.AreEqual(900m, table[0].Values[1]);
    }

    #endregion

    #region Category 4: Complex CTE + Reordered + Set Operators

    [TestMethod]
    public void CteWithReorderedQuery_UnionOperator_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() select City, Country
                union (City, Country)
                from #B.Entities() select City, Country
            )
            select City, Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            },
            {
                "#B", [
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "WARSAW"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "BERLIN"));
    }

    [TestMethod]
    public void CteWithReorderedQuery_ExceptOperator_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() select City, Country
                except (Country)
                from #B.Entities() select City, Country
            )
            select City, Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            },
            {
                "#B", [
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", table[0].Values[0]);
        Assert.AreEqual("POLAND", table[0].Values[1]);
    }

    [TestMethod]
    public void CteWithReorderedQuery_IntersectOperator_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() select City, Country
                intersect (Country)
                from #B.Entities() select City, Country
            )
            select City, Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            },
            {
                "#B", [
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("BERLIN", table[0].Values[0]);
        Assert.AreEqual("GERMANY", table[0].Values[1]);
    }

    [TestMethod]
    public void CteWithReorderedQuery_UnionAllOperator_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() select City, Country
                union all (City)
                from #B.Entities() select City, Country
            )
            select City, Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            },
            {
                "#B", [
                    new BasicEntity("WARSAW", "POLAND", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.All(row => (string)row.Values[0] == "WARSAW"));
    }

    #endregion

    #region Category 5: CTE + Reordered + JOIN Operations

    [TestMethod]
    public void CteWithReorderedQuery_LeftOuterJoin_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() a 
                left outer join #B.Entities() b on a.Country = b.Country 
                select a.City as City, a.Country as Country, b.City as OtherCity
            )
            select City, Country, OtherCity from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            },
            {
                "#B", [
                    new BasicEntity("KRAKOW", "POLAND", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "WARSAW" &&
            (string)row.Values[2] == "KRAKOW"));
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "BERLIN" &&
            row.Values[2] == null));
    }

    [TestMethod]
    public void CteWithReorderedQuery_MultipleJoins_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() a 
                inner join #B.Entities() b on a.Country = b.Country 
                inner join #C.Entities() c on b.Country = c.Country
                select a.City as CityA, b.City as CityB, c.City as CityC
            )
            select CityA, CityB, CityC from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            },
            {
                "#B", [
                    new BasicEntity("KRAKOW", "POLAND", 300)
                ]
            },
            {
                "#C", [
                    new BasicEntity("GDANSK", "POLAND", 200)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", table[0].Values[0]);
        Assert.AreEqual("KRAKOW", table[0].Values[1]);
        Assert.AreEqual("GDANSK", table[0].Values[2]);
    }

    [TestMethod]
    public void CteReferencingAnotherCte_WithReorderedJoin_ShouldWork()
    {
        var query = @"
            with 
                cte1 as (
                    from #A.Entities() select City, Country, Population
                ),
                cte2 as (
                    from cte1 a 
                    inner join #B.Entities() b on a.Country = b.Country 
                    select a.City as CityA, b.City as CityB, a.Population as Population
                )
            from cte2 select CityA, CityB, Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            },
            {
                "#B", [
                    new BasicEntity("KRAKOW", "POLAND", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", table[0].Values[0]);
        Assert.AreEqual("KRAKOW", table[0].Values[1]);
    }

    #endregion

    #region Category 6: Mixed Syntax (Reordered + Standard)

    [TestMethod]
    public void MixedSyntax_StandardCteReorderedOuter_ShouldWork()
    {
        var query = @"
            with cte as (
                select City, Country, Population from #A.Entities()
            )
            from cte where Population > 300 select City, Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void MixedSyntax_ReorderedCteStandardOuter_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() where Population > 300 select City, Country, Population
            )
            select City, Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void MixedSyntax_MultipleCtesMixedStyles_ShouldWork()
    {
        var query = @"
            with 
                cte1 as (
                    from #A.Entities() where Country = 'POLAND' select City, Country
                ),
                cte2 as (
                    select City, Country from #B.Entities() where Country = 'GERMANY'
                )
            select City, Country from cte1
            union (City, Country)
            select City, Country from cte2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            },
            {
                "#B", [
                    new BasicEntity("MUNICH", "GERMANY", 350),
                    new BasicEntity("KRAKOW", "POLAND", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "WARSAW"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "MUNICH"));
    }

    [TestMethod]
    public void MixedSyntax_SetOperatorMixedStyles_ShouldWork()
    {
        var query = @"
            from #A.Entities() select City, Country
            union (City, Country)
            select City, Country from #B.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            },
            {
                "#B", [
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void MixedSyntax_CteChainMixedStyles_ShouldWork()
    {
        var query = @"
            with 
                first as (
                    from #A.Entities() select City, Country, Population
                ),
                second as (
                    select City, Country, Population from first where Population > 300
                ),
                third as (
                    from second group by Country select Country, Sum(Population) as TotalPop
                )
            select Country, TotalPop from third";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "POLAND" &&
            (decimal)row.Values[1] == 900m));
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "GERMANY" &&
            (decimal)row.Values[1] == 350m));
    }

    #endregion

    #region Category 7: Complex Nested Scenarios

    [TestMethod]
    public void NestedCtes_ReferencingOtherCtes_AllReordered_ShouldWork()
    {
        var query = @"
            with 
                base as (
                    from #A.Entities() select City, Country, Population
                ),
                filtered as (
                    from base where Population > 300 select City, Country, Population
                ),
                aggregated as (
                    from filtered group by Country select Country, Count(City) as CityCount
                )
            from aggregated select Country, CityCount";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "POLAND" &&
            (int)row.Values[1] == 2));
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "GERMANY" &&
            (int)row.Values[1] == 1));
    }

    [TestMethod]
    public void ComplexQuery_CteReorderedJoinGroupByOrderBy_ShouldWork()
    {
        var query = @"
            with 
                cities as (
                    from #A.Entities() a
                    inner join #B.Entities() b on a.Country = b.Country
                    select a.City as City, a.Country as Country, a.Population as Population, b.Population as OtherPop
                )
            from cities 
            group by Country 
            select Country, Sum(Population) as TotalPop, Count(City) as CityCount
            order by Sum(Population) desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            },
            {
                "#B", [
                    new BasicEntity("KRAKOW", "POLAND", 300),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "POLAND" &&
            (decimal)row.Values[1] == 900m));
    }

    [TestMethod]
    public void ComplexQuery_MultipleCtesDifferentOperations_ShouldWork()
    {
        var query = @"
            with 
                polish as (
                    from #A.Entities() where Country = 'POLAND' select City, Country, Population
                ),
                german as (
                    from #A.Entities() where Country = 'GERMANY' select City, Country, Population
                ),
                combined as (
                    from polish select City, Country, Population
                    union all (City)
                    from german select City, Country, Population
                ),
                summary as (
                    from combined group by Country select Country, Sum(Population) as TotalPop
                )
            from summary select Country, TotalPop order by TotalPop desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "POLAND" &&
            (decimal)row.Values[1] == 900m));
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "GERMANY" &&
            (decimal)row.Values[1] == 600m));
    }

    #endregion

    #region Category 8: Edge Cases

    [TestMethod]
    public void ReorderedQuery_WithDistinct_InCte_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() select distinct Country
            )
            select Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void ReorderedQuery_WithCaseWhen_InCte_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() 
                select City, 
                       case when Population > 400 then 'Large' else 'Small' end as Size
            )
            select City, Size from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("KATOWICE", "POLAND", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "WARSAW" &&
            (string)row.Values[1] == "Large"));
        Assert.IsTrue(table.Any(row =>
            (string)row.Values[0] == "KATOWICE" &&
            (string)row.Values[1] == "Small"));
    }

    [TestMethod]
    public void ReorderedQuery_WithArithmeticExpressions_InCte_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() 
                select City, Population * 2 as DoubledPop, Population + 100 as IncreasedPop
            )
            select City, DoubledPop, IncreasedPop from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", table[0].Values[0]);
        Assert.AreEqual(1000m, table[0].Values[1]);
        Assert.AreEqual(600m, table[0].Values[2]);
    }

    [TestMethod]
    public void ReorderedQuery_WithStar_InCte_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() where Country = 'POLAND' select *
            )
            select City, Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", table[0].Values[0]);
    }

    [TestMethod]
    public void ReorderedQuery_WithAliasedStar_InCte_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() a where a.Country = 'POLAND' select a.*
            )
            select [a.City], [a.Country] from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", table[0].Values[0]);
    }

    [TestMethod]
    public void ReorderedQuery_ComplexWhereConditions_InCte_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() 
                where Country = 'POLAND' and Population > 300 or Country = 'GERMANY'
                select City, Country, Population
            )
            select City, Country from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "WARSAW"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "BERLIN"));
    }

    [TestMethod]
    public void ReorderedQuery_WithFunctionCalls_InCte_ShouldWork()
    {
        var query = @"
            with cte as (
                from #A.Entities() 
                select ToUpper(City) as UpperCity, ToLower(Country) as LowerCountry
            )
            select UpperCity, LowerCountry from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Warsaw", "Poland", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", table[0].Values[0]);
        Assert.AreEqual("poland", table[0].Values[1]);
    }

    #endregion
}
