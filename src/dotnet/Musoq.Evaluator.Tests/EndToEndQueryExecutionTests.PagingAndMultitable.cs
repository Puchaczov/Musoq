using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class EndToEndQueryExecutionTests
{
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

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)), ("Cnt", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["NYC", 2L]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A"], ["B"], ["C"], ["D"]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("f.Name", typeof(string)), ("b.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A", "B"]);
    }



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

        TableMaterializationTestHelper.AssertColumns(table, ("Population % 3", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1m], [0m], [2m]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name +  -  + City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Test - NYC"]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("Substring(Name, 0, 2)", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Te"]);
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

        TableMaterializationTestHelper.AssertColumns(table, ("-1 * Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [-100m]);
    }

}
