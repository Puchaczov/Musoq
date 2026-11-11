using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class ReorderedSyntaxCteTests
{
    [TestMethod]
    public void MultipleCtes_AllReordered_ShouldWork()
    {
        var query = (
            "\n"
            + "            with\n"
            + "                cte1 as (\n"
            + "                    from #A.Entities() where Country = 'POLAND' select City, Country, Population\n"
            + "                ),\n"
            + "                cte2 as (\n"
            + "                    from #B.Entities() where Country = 'GERMANY' select City, Country, Population\n"
            + "                )\n"
            + "            from cte1 select City, Country\n"
            + "            union (City, Country)\n"
            + "            from cte2 select City, Country"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("WARSAW", "POLAND", 500), new BasicEntity("BERLIN", "GERMANY", 250)] },
            { "#B", [new BasicEntity("MUNICH", "GERMANY", 350), new BasicEntity("KRAKOW", "POLAND", 300)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "POLAND"], ["MUNICH", "GERMANY"]);
    }

    [TestMethod]
    public void MultipleCtes_ReorderedWithGroupBy_ShouldWork()
    {
        var query = (
            "\n"
            + "            with\n"
            + "                cte1 as (\n"
            + "                    from #A.Entities() group by Country select Country, Sum(Population) as TotalPop\n"
            + "                ),\n"
            + "                cte2 as (\n"
            + "                    from cte1 where TotalPop > 500 select Country, TotalPop\n"
            + "                )\n"
            + "            from cte2 select Country, TotalPop"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("TotalPop", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["POLAND", 900m]);
    }

    [TestMethod]
    public void CteWithReorderedQuery_UnionOperator_ShouldWork()
    {
        var query = (
            "\n"
            + "            with cte as (\n"
            + "                from #A.Entities() select City, Country\n"
            + "                union (City, Country)\n"
            + "                from #B.Entities() select City, Country\n"
            + "            )\n"
            + "            select City, Country from cte"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("WARSAW", "POLAND", 500)] },
            { "#B", [new BasicEntity("BERLIN", "GERMANY", 250)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "POLAND"], ["BERLIN", "GERMANY"]);
    }

    [TestMethod]
    public void CteWithReorderedQuery_ExceptOperator_ShouldWork()
    {
        var query = (
            "\n"
            + "            with cte as (\n"
            + "                from #A.Entities() select City, Country\n"
            + "                except (Country)\n"
            + "                from #B.Entities() select City, Country\n"
            + "            )\n"
            + "            select City, Country from cte"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("WARSAW", "POLAND", 500), new BasicEntity("BERLIN", "GERMANY", 250)] },
            { "#B", [new BasicEntity("MUNICH", "GERMANY", 350)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "POLAND"]);
    }

    [TestMethod]
    public void CteWithReorderedQuery_IntersectOperator_ShouldWork()
    {
        var query = (
            "\n"
            + "            with cte as (\n"
            + "                from #A.Entities() select City, Country\n"
            + "                intersect (Country)\n"
            + "                from #B.Entities() select City, Country\n"
            + "            )\n"
            + "            select City, Country from cte"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("WARSAW", "POLAND", 500), new BasicEntity("BERLIN", "GERMANY", 250)] },
            { "#B", [new BasicEntity("MUNICH", "GERMANY", 350)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["BERLIN", "GERMANY"]);
    }

    [TestMethod]
    public void CteWithReorderedQuery_UnionAllOperator_ShouldWork()
    {
        var query = (
            "\n"
            + "            with cte as (\n"
            + "                from #A.Entities() select City, Country\n"
            + "                union all (City)\n"
            + "                from #B.Entities() select City, Country\n"
            + "            )\n"
            + "            select City, Country from cte"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("WARSAW", "POLAND", 500)] },
            { "#B", [new BasicEntity("WARSAW", "POLAND", 300)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "POLAND"], ["WARSAW", "POLAND"]);
    }

    [TestMethod]
    public void CteWithReorderedQuery_LeftOuterJoin_ShouldWork()
    {
        var query = (
            "\n"
            + "            with cte as (\n"
            + "                from #A.Entities() a\n"
            + "                left outer join #B.Entities() b on a.Country = b.Country\n"
            + "                select a.City as City, a.Country as Country, b.City as OtherCity\n"
            + "            )\n"
            + "            select City, Country, OtherCity from cte"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("WARSAW", "POLAND", 500), new BasicEntity("BERLIN", "GERMANY", 250)] },
            { "#B", [new BasicEntity("KRAKOW", "POLAND", 300)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Country", typeof(string)),
            ("OtherCity", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["WARSAW", "POLAND", "KRAKOW"],
            ["BERLIN", "GERMANY", null]
        );
    }

    [TestMethod]
    public void CteWithReorderedQuery_MultipleJoins_ShouldWork()
    {
        var query = (
            "\n"
            + "            with cte as (\n"
            + "                from #A.Entities() a\n"
            + "                inner join #B.Entities() b on a.Country = b.Country\n"
            + "                inner join #C.Entities() c on b.Country = c.Country\n"
            + "                select a.City as CityA, b.City as CityB, c.City as CityC\n"
            + "            )\n"
            + "            select CityA, CityB, CityC from cte"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("WARSAW", "POLAND", 500)] },
            { "#B", [new BasicEntity("KRAKOW", "POLAND", 300)] },
            { "#C", [new BasicEntity("GDANSK", "POLAND", 200)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("CityA", typeof(string)),
            ("CityB", typeof(string)),
            ("CityC", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "KRAKOW", "GDANSK"]);
    }

    [TestMethod]
    public void CteReferencingAnotherCte_WithReorderedJoin_ShouldWork()
    {
        var query = (
            "\n"
            + "            with\n"
            + "                cte1 as (\n"
            + "                    from #A.Entities() select City, Country, Population\n"
            + "                ),\n"
            + "                cte2 as (\n"
            + "                    from cte1 a\n"
            + "                    inner join #B.Entities() b on a.Country = b.Country\n"
            + "                    select a.City as CityA, b.City as CityB, a.Population as Population\n"
            + "                )\n"
            + "            from cte2 select CityA, CityB, Population"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("WARSAW", "POLAND", 500)] },
            { "#B", [new BasicEntity("KRAKOW", "POLAND", 300)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("CityA", typeof(string)),
            ("CityB", typeof(string)),
            ("Population", typeof(decimal))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["WARSAW", "KRAKOW", 500m]);
    }
}
