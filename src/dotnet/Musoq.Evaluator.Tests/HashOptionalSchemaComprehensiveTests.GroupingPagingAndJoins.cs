using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class HashOptionalSchemaComprehensiveTests
{
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

}
