using System;
using System.Collections.Generic;
using System.Linq;
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

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
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

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (string)r[0]).ToList();

        Assert.AreEqual("UK", rows[0][0]);
        Assert.AreEqual(1L, (long)rows[0][1]);

        Assert.AreEqual("US", rows[1][0]);
        Assert.AreEqual(2L, (long)rows[1][1]);
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

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("A3", table[0][0]);
        Assert.AreEqual("A2", table[1][0]);
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

        Assert.AreEqual(0, table.Count);
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

        Assert.AreEqual(0, table.Count);
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

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
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

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
        Assert.AreEqual("C1", table[0][2]);
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

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (string)r[0]).ToList();

        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B1", rows[0][1]);

        Assert.AreEqual("A2", rows[1][0]);
        Assert.AreEqual("B2", rows[1][1]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithPartitionColumn_ShouldCorrelatePerService()
    {
        var query = @"
select
    errors.Name,
    errors.Time,
    deploys.Name,
    deploys.Time
from #A.entities() errors
asof join #B.entities() deploys on errors.Country = deploys.Country and errors.Time >= deploys.Time";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "Error-Auth", Country = "auth-svc", Time = new DateTime(2025, 3, 10, 14, 30, 0) },
                    new BasicEntity { Name = "Error-Pay",  Country = "pay-svc",  Time = new DateTime(2025, 3, 10, 15, 0, 0) },
                    new BasicEntity { Name = "Error-Auth2", Country = "auth-svc", Time = new DateTime(2025, 3, 10, 10, 0, 0) }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Deploy-Auth-v2", Country = "auth-svc", Time = new DateTime(2025, 3, 10, 14, 0, 0) },
                    new BasicEntity { Name = "Deploy-Auth-v1", Country = "auth-svc", Time = new DateTime(2025, 3, 10, 9, 0, 0) },
                    new BasicEntity { Name = "Deploy-Pay-v1",  Country = "pay-svc",  Time = new DateTime(2025, 3, 10, 12, 0, 0) }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var rows = table.OrderBy(r => (string)r[0]).ToList();

        // Error-Auth (auth-svc, 14:30) -> Deploy-Auth-v2 (auth-svc, 14:00) — most recent deploy before error
        Assert.AreEqual("Error-Auth", rows[0][0]);
        Assert.AreEqual("Deploy-Auth-v2", rows[0][2]);

        // Error-Auth2 (auth-svc, 10:00) -> Deploy-Auth-v1 (auth-svc, 9:00) — only deploy before this error
        Assert.AreEqual("Error-Auth2", rows[1][0]);
        Assert.AreEqual("Deploy-Auth-v1", rows[1][2]);

        // Error-Pay (pay-svc, 15:00) -> Deploy-Pay-v1 (pay-svc, 12:00) — only pay-svc deploy
        Assert.AreEqual("Error-Pay", rows[2][0]);
        Assert.AreEqual("Deploy-Pay-v1", rows[2][2]);
    }
}
