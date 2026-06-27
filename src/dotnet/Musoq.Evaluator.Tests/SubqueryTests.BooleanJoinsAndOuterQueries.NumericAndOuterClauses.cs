using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenInSubquery_WithNumericColumn_ShouldWork()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.Population IN (SELECT b.Population FROM #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            },
            {
                "#B", [
                    new BasicEntity("X", "Y", 500),
                    new BasicEntity("X", "Y", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "WARSAW"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "PARIS"));
    }

    [TestMethod]
    public void WhenNotInSubquery_WithNumericColumn_ShouldWork()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.Population NOT IN (SELECT b.Population FROM #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            },
            {
                "#B", [
                    new BasicEntity("X", "Y", 500),
                    new BasicEntity("X", "Y", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("BERLIN", (string)table[0].Values[0]);
    }

    [TestMethod]
    public void WhenInSubquery_WithOrderBy_ShouldPreserveOrder()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b)
            ORDER BY a.City ASC";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            },
            {
                "#B", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("BERLIN", (string)table[0].Values[0]);
        Assert.AreEqual("PARIS", (string)table[1].Values[0]);
        Assert.AreEqual("WARSAW", (string)table[2].Values[0]);
    }

    [TestMethod]
    public void WhenInSubquery_WithSkipTake_ShouldWork()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b)
            ORDER BY a.City ASC
            SKIP 1 TAKE 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            },
            {
                "#B", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("PARIS", (string)table[0].Values[0]);
    }

    [TestMethod]
    public void WhenInSubquery_WithGroupByInOuterQuery_ShouldWork()
    {
        var query = @"
            SELECT a.Country, Count(a.Country) FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b)
            GROUP BY a.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("KRAKOW", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            },
            {
                "#B", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("KRAKOW", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "POLAND" && (long)r.Values[1] == 2L));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "GERMANY" && (long)r.Values[1] == 1L));
    }

    [TestMethod]
    public void WhenInSubquery_WithHavingInOuterQuery_ShouldWork()
    {
        var query = @"
            SELECT a.Country, Count(a.Country) FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b)
            GROUP BY a.Country
            HAVING Count(a.Country) > 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("KRAKOW", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            },
            {
                "#B", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("KRAKOW", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("POLAND", (string)table[0].Values[0]);
        Assert.AreEqual(2L, (long)table[0].Values[1]);
    }

    [TestMethod]
    public void WhenInSubquery_WithDistinctInOuterQuery_ShouldWork()
    {
        var query = @"
            SELECT DISTINCT a.Country FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("KRAKOW", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            },
            {
                "#B", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("KRAKOW", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "POLAND"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "GERMANY"));
    }
}
