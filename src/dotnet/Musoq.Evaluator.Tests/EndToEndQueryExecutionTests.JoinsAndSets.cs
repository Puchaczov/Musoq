using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class EndToEndQueryExecutionTests
{
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
            cross join #B.Entities() b";
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



}
