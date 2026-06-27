using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CteTests
{
    [TestMethod]
    public void WhenMixedQueriesGroupingAreUsed_Variant1_ShouldPass()
    {
        var query = @"
with first as (
    select 1 as Test from #A.entities()
), second as (
    select 2 as Test from #B.entities() group by 'fake'
)
select c.GetBytes(c.Name) from first a
    inner join #A.entities() c on 1 = 1
    inner join second b on 1 = 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("First")
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("Second")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Table should have 1 entry");
        Assert.IsTrue(table.Any(entry =>
            Encoding.UTF8.GetString((byte[])entry.Values[0]) == "First"
        ), "First entry should be 'First'");
    }

    [TestMethod]
    public void WhenMixedQueriesGroupingAreUsed_Variant2_ShouldPass()
    {
        var query = @"
with first as (
    select Name from #A.entities()
), second as (
    select Name from #B.entities() group by Name
)
select
    c.GetBytes(c.Name)
from first a
    inner join #A.entities() c on 1 = 1
    inner join second b on 1 = 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("First")
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("Second")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("First", Encoding.UTF8.GetString((byte[])table[0].Values[0]));
    }

    [TestMethod]
    public void WhenMixedQueriesGroupingAreUsed_Variant3_ShouldPass()
    {
        var query = @"
with first as (
    select Name from #A.entities()
), second as (
    select Name from #B.entities() group by Name
)
select
    a.Name,
    b.Name,
    c.GetBytes(c.Name)
from first a
    inner join #A.entities() c on 1 = 1
    inner join second b on 1 = 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("First")
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("Second")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("First", (string)table[0].Values[0]);
        Assert.AreEqual("Second", (string)table[0].Values[1]);
        Assert.AreEqual("First", Encoding.UTF8.GetString((byte[])table[0].Values[2]));
    }

    [TestMethod]
    public void WhenMixedQueriesGroupingAreUsed_Variant4_ShouldPass()
    {
        var query = @"
with first as (
    select a.Name as Name from #A.entities() a group by a.Name having Count(a.Name) > 0
), second as (
    select b.Name() as Name from #B.entities() b inner join first a on 1 = 1
), third as (
    select b.Name as Name from second b group by b.Name having Count(b.Name) > 0
), fourth as (
    select
        c.Name() as Name
    from second b
        inner join #A.entities() c on 1 = 1
        inner join third d on 1 = 1
)
select
    c.Name as Name
from fourth c";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("First")
                ]
            },
            {
                "#B",
                [
                    new BasicEntity("Second")
                ]
            },
            {
                "#C",
                [
                    new BasicEntity("Third")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("First", (string)table[0].Values[0]);
    }
}
