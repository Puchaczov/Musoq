using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class AsOfJoinTests
{
    [TestMethod]
    public void WhenAsOfJoinWithWhereClause_ShouldFilterAfterJoin()
    {
        var query = @"
select
    a.Name,
    b.Name
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population
where a.Name <> 'A2'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 90 },
                    new BasicEntity { Name = "B2", Population = 40 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A1", "B1"]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithGroupBy_ShouldAggregateAfterJoin()
    {
        var query = @"
select
    a.Country,
    Count(a.Name)
from #A.entities() a
asof join #B.entities() b on a.Country = b.Country and a.Population >= b.Population
group by a.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Country = "US", Population = 100 },
                    new BasicEntity { Name = "A2", Country = "US", Population = 80 },
                    new BasicEntity { Name = "A3", Country = "UK", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Country = "US", Population = 70 },
                    new BasicEntity { Name = "B2", Country = "UK", Population = 40 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Country", typeof(string)),
            ("Count(a.Name)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["UK", 1L],
            ["US", 2L]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithOrderByAndTake_ShouldApplyAfterJoin()
    {
        var query = @"
select
    a.Name,
    b.Name
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population
order by a.Name desc
take 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 80 },
                    new BasicEntity { Name = "A3", Population = 60 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 50 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["A3", "B1"],
            ["A2", "B1"]);
    }

    [TestMethod]
    public void WhenAsOfJoinLeftSideEmpty_ShouldReturnEmpty()
    {
        var query = @"
select
    a.Name,
    b.Name
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", Array.Empty<BasicEntity>()
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);
    }

    [TestMethod]
    public void WhenAsOfJoinBothSidesEmpty_ShouldReturnEmpty()
    {
        var query = @"
select
    a.Name,
    b.Name
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", Array.Empty<BasicEntity>()
            },
            {
                "#B", Array.Empty<BasicEntity>()
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);
    }

    [TestMethod]
    public void WhenAsOfJoinWithCte_ShouldWorkCorrectly()
    {
        var query = @"
with leftCte as (
    select Name, Population from #A.entities()
),
rightCte as (
    select Name, Population from #B.entities()
)
select l.Name, r.Name
from leftCte l
asof join rightCte r on l.Population >= r.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 90 },
                    new BasicEntity { Name = "B2", Population = 50 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("l.Name", typeof(string)),
            ("r.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A1", "B1"]);
    }

    [TestMethod]
    public void WhenAsOfJoinChainedWithInnerJoin_ShouldWorkCorrectly()
    {
        var query = @"
select
    a.Name,
    b.Name,
    c.Name
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population
inner join #C.entities() c on a.Country = c.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100, Country = "US" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 90 }
                ]
            },
            {
                "#C", [
                    new BasicEntity { Name = "C1", Country = "US" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)),
            ("c.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A1", "B1", "C1"]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithMultipleEqualityKeys_ShouldPartitionByCompositeKey()
    {
        var query = @"
select
    a.Name,
    b.Name
from #A.entities() a
asof join #B.entities() b on a.Country = b.Country and a.City = b.City and a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Country = "US", City = "NYC", Population = 100 },
                    new BasicEntity { Name = "A2", Country = "US", City = "LA", Population = 80 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Country = "US", City = "NYC", Population = 90 },
                    new BasicEntity { Name = "B2", Country = "US", City = "LA", Population = 70 },
                    new BasicEntity { Name = "B3", Country = "US", City = "NYC", Population = 50 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A1", "B1"],
            ["A2", "B2"]);
    }

}
