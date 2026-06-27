using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class AsOfJoinTests
{
    [TestMethod]
    public void WhenAsOfJoinWithGreaterOrEqual_ShouldReturnClosestMatch()
    {
        var query = @"
select
    a.Name,
    a.Population,
    b.Name,
    b.Population
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 50 },
                    new BasicEntity { Name = "A3", Population = 10 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 90 },
                    new BasicEntity { Name = "B2", Population = 40 },
                    new BasicEntity { Name = "B3", Population = 5 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var rows = table.OrderByDescending(r => (decimal)r[1]).ToList();

        // A1 (100) >= B1 (90) — closest match
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B1", rows[0][2]);

        // A2 (50) >= B2 (40) — closest match
        Assert.AreEqual("A2", rows[1][0]);
        Assert.AreEqual("B2", rows[1][2]);

        // A3 (10) >= B3 (5) — closest match
        Assert.AreEqual("A3", rows[2][0]);
        Assert.AreEqual("B3", rows[2][2]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithGreaterThan_ShouldReturnStrictlyLessMatch()
    {
        var query = @"
select
    a.Name,
    a.Population,
    b.Name,
    b.Population
from #A.entities() a
asof join #B.entities() b on a.Population > b.Population";

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
                    new BasicEntity { Name = "B1", Population = 100 },
                    new BasicEntity { Name = "B2", Population = 50 },
                    new BasicEntity { Name = "B3", Population = 30 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderByDescending(r => (decimal)r[1]).ToList();

        // A1 (100) > B2 (50) — closest strictly less
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B2", rows[0][2]);

        // A2 (50) > B3 (30) — closest strictly less
        Assert.AreEqual("A2", rows[1][0]);
        Assert.AreEqual("B3", rows[1][2]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithLessThanOrEqual_ShouldReturnClosestGreaterOrEqual()
    {
        var query = @"
select
    a.Name,
    a.Population,
    b.Name,
    b.Population
from #A.entities() a
asof join #B.entities() b on a.Population <= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 10 },
                    new BasicEntity { Name = "A2", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 20 },
                    new BasicEntity { Name = "B2", Population = 60 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (decimal)r[1]).ToList();

        // A1 (10) <= B1 (20) — smallest right >= left
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B1", rows[0][2]);

        // A2 (50) <= B2 (60) — smallest right >= left
        Assert.AreEqual("A2", rows[1][0]);
        Assert.AreEqual("B2", rows[1][2]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithLessThan_ShouldReturnClosestStrictlyGreater()
    {
        var query = @"
select
    a.Name,
    a.Population,
    b.Name,
    b.Population
from #A.entities() a
asof join #B.entities() b on a.Population < b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 10 },
                    new BasicEntity { Name = "A2", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 10 },
                    new BasicEntity { Name = "B2", Population = 50 },
                    new BasicEntity { Name = "B3", Population = 60 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (decimal)r[1]).ToList();

        // A1 (10) < B2 (50) — smallest right strictly > left
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B2", rows[0][2]);

        // A2 (50) < B3 (60) — smallest right strictly > left
        Assert.AreEqual("A2", rows[1][0]);
        Assert.AreEqual("B3", rows[1][2]);
    }

    [TestMethod]
    public void WhenAsOfJoinNoMatch_ShouldReturnEmpty()
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
                "#A", [
                    new BasicEntity { Name = "A1", Population = 1 }
                ]
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
    public void WhenAsOfLeftJoinNoMatch_ShouldReturnLeftWithNulls()
    {
        var query = @"
select
    a.Name,
    b.Name
from #A.entities() a
asof left join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 1 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    [TestMethod]
    public void WhenAsOfLeftJoinWithMatch_ShouldReturnClosestMatch()
    {
        var query = @"
select
    a.Name,
    a.Population,
    b.Name,
    b.Population
from #A.entities() a
asof left join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 1 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 50 },
                    new BasicEntity { Name = "B2", Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderByDescending(r => (decimal)r[1]).ToList();

        // A1 (100) >= B1 (50) — match
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B1", rows[0][2]);

        // A2 (1) — no B <= 1, null
        Assert.AreEqual("A2", rows[1][0]);
        Assert.IsNull(rows[1][2]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithEqualityAndInequality_ShouldPartitionByEqualityKey()
    {
        var query = @"
select
    a.Name,
    a.Country,
    a.Population,
    b.Name,
    b.Country,
    b.Population
from #A.entities() a
asof join #B.entities() b on a.Country = b.Country and a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Country = "US", Population = 100 },
                    new BasicEntity { Name = "A2", Country = "UK", Population = 80 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Country = "US", Population = 90 },
                    new BasicEntity { Name = "B2", Country = "US", Population = 50 },
                    new BasicEntity { Name = "B3", Country = "UK", Population = 70 },
                    new BasicEntity { Name = "B4", Country = "UK", Population = 30 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderByDescending(r => (decimal)r[2]).ToList();

        // A1 (US, 100) matched with B1 (US, 90) — closest US match
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B1", rows[0][3]);

        // A2 (UK, 80) matched with B3 (UK, 70) — closest UK match
        Assert.AreEqual("A2", rows[1][0]);
        Assert.AreEqual("B3", rows[1][3]);
    }

    [TestMethod]
    public void WhenAsOfLeftJoinWithEqualityNoMatch_ShouldReturnNulls()
    {
        var query = @"
select
    a.Name,
    a.Country,
    b.Name
from #A.entities() a
asof left join #B.entities() b on a.Country = b.Country and a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Country = "US", Population = 100 },
                    new BasicEntity { Name = "A2", Country = "FR", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Country = "US", Population = 90 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (string)r[0]).ToList();

        // A1 (US, 100) matches B1 (US, 90)
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B1", rows[0][2]);

        // A2 (FR, 50) — no FR in B, null
        Assert.AreEqual("A2", rows[1][0]);
        Assert.IsNull(rows[1][2]);
    }
}
