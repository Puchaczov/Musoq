using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class AsOfJoinTests
{
    [TestMethod]
    public void WhenAsOfJoinExactMatch_ShouldReturnExactMatch()
    {
        var query = @"
select
    a.Name,
    b.Name,
    a.Population,
    b.Population
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 50 },
                    new BasicEntity { Name = "B2", Population = 30 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        // A1 (50) >= B1 (50) — exact match preferred
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
    }

    [TestMethod]
    public void WhenAsOfLeftOuterJoin_ShouldWorkSameAsAsOfLeftJoin()
    {
        var query = @"
select
    a.Name,
    b.Name
from #A.entities() a
asof left outer join #B.entities() b on a.Population >= b.Population";

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
    public void WhenAsOfJoinEmptyRight_ShouldReturnEmpty()
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
                    new BasicEntity { Name = "A1", Population = 100 }
                ]
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
    public void WhenAsOfLeftJoinEmptyRight_ShouldReturnLeftWithNulls()
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
                    new BasicEntity { Name = "A1", Population = 100 }
                ]
            },
            {
                "#B", Array.Empty<BasicEntity>()
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithDateTimeColumn_ShouldMatchByTime()
    {
        var query = @"
select
    a.Name,
    b.Name
from #A.entities() a
asof join #B.entities() b on a.Time >= b.Time";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "Error1", Time = new DateTime(2025, 1, 15, 14, 30, 0) },
                    new BasicEntity { Name = "Error2", Time = new DateTime(2025, 1, 15, 10, 0, 0) }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Commit1", Time = new DateTime(2025, 1, 15, 14, 0, 0) },
                    new BasicEntity { Name = "Commit2", Time = new DateTime(2025, 1, 15, 9, 0, 0) },
                    new BasicEntity { Name = "Commit3", Time = new DateTime(2025, 1, 14, 12, 0, 0) }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (string)r[0]).ToList();

        Assert.AreEqual("Error1", rows[0][0]);
        Assert.AreEqual("Commit1", rows[0][1]);

        Assert.AreEqual("Error2", rows[1][0]);
        Assert.AreEqual("Commit2", rows[1][1]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithStringColumn_ShouldMatchLexicographically()
    {
        var query = @"
select
    a.Name,
    b.Name
from #A.entities() a
asof join #B.entities() b on a.City >= b.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", City = "M" }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", City = "A" },
                    new BasicEntity { Name = "B2", City = "K" },
                    new BasicEntity { Name = "B3", City = "Z" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B2", table[0][1]);
    }
}
