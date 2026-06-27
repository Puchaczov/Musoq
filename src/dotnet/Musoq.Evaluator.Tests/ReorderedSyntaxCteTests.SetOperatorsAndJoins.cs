using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class ReorderedSyntaxCteTests
{
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



}
