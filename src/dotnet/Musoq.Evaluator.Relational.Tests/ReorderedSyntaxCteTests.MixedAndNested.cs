using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class ReorderedSyntaxCteTests
{
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

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "POLAND"], ["CZESTOCHOWA", "POLAND"]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "POLAND"], ["CZESTOCHOWA", "POLAND"]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "POLAND"], ["MUNICH", "GERMANY"]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "POLAND"], ["BERLIN", "GERMANY"]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)), ("TotalPop", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["POLAND", 900m], ["GERMANY", 350m]);
    }



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

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)), ("CityCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["POLAND", 2L], ["GERMANY", 1L]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)), ("TotalPop", typeof(decimal?)), ("CityCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["POLAND", 900m, 2L], ["GERMANY", 250m, 1L]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)), ("TotalPop", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["POLAND", 900m], ["GERMANY", 600m]);
    }



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

        TableMaterializationTestHelper.AssertColumns(table, ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["POLAND"], ["GERMANY"]);
    }

}
