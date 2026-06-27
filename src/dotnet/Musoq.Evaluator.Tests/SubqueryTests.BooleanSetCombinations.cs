using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenNotInSubquery_WithOrCondition_ShouldReturnMatchingRows()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City NOT IN (SELECT b.City FROM #B.entities() b) OR a.Country = 'GERMANY'";

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
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "BERLIN"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "PARIS"));
    }

    [TestMethod]
    public void WhenInSubquery_WithOrBetweenTwoInSubqueries_ShouldReturnUnion()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b)
               OR a.Country IN (SELECT c.Country FROM #C.entities() c)";

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
                "#B", [new BasicEntity("PARIS", "FRANCE", 300)]
            },
            {
                "#C", [new BasicEntity("BERLIN", "GERMANY", 250)]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "PARIS"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "BERLIN"));
    }

    [TestMethod]
    public void WhenInSubquery_WithAndContainingOrWithSubquery_ShouldWork()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b)
              AND (a.Population > 400 OR a.Country IN (SELECT c.Country FROM #C.entities() c))";

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
            },
            {
                "#C", [new BasicEntity("BERLIN", "GERMANY", 250)]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "WARSAW"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "BERLIN"));
    }

    [TestMethod]
    public void WhenInSubquery_OrWithNoMatchingRows_ShouldReturnOnlyOrBranch()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b) OR a.Population > 400";

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
                "#B", [new BasicEntity("TOKYO", "JAPAN", 1000)]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", (string)table[0].Values[0]);
    }

    // ── Set operator IN subquery tests ────────────────────────────────────────

    [TestMethod]
    public void WhenNotInSubquery_WithUnion_ShouldExcludeUnionResults()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City NOT IN (SELECT b.City FROM #B.entities() b UNION (City) SELECT c.City FROM #C.entities() c)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PRAGUE", "CZECH", 200)
                ]
            },
            {
                "#B", [new BasicEntity("WARSAW", "POLAND", 500)]
            },
            {
                "#C", [new BasicEntity("BERLIN", "GERMANY", 250)]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("PRAGUE", (string)table[0].Values[0]);
    }

    [TestMethod]
    public void WhenInSubquery_WithChainedUnions_ShouldCombineAll()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (
                SELECT b.City FROM #B.entities() b
                UNION (City) SELECT c.City FROM #C.entities() c
                UNION (City) SELECT d.City FROM #D.entities() d)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300),
                    new BasicEntity("LONDON", "UK", 400)
                ]
            },
            {
                "#B", [new BasicEntity("WARSAW", "POLAND", 500)]
            },
            {
                "#C", [new BasicEntity("BERLIN", "GERMANY", 250)]
            },
            {
                "#D", [new BasicEntity("PARIS", "FRANCE", 300)]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "WARSAW"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "BERLIN"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "PARIS"));
    }

    [TestMethod]
    public void WhenInSubquery_WithUnionAndOr_ShouldWork()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b UNION (City) SELECT c.City FROM #C.entities() c)
               OR a.Country = 'FRANCE'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300),
                    new BasicEntity("LONDON", "UK", 400)
                ]
            },
            {
                "#B", [new BasicEntity("WARSAW", "POLAND", 500)]
            },
            {
                "#C", [new BasicEntity("BERLIN", "GERMANY", 250)]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "WARSAW"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "BERLIN"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "PARIS"));
    }

    [TestMethod]
    public void WhenInSubquery_WithExceptAndMultipleColumns_ShouldFilterCorrectly()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (
                SELECT b.City FROM #B.entities() b
                EXCEPT (City) SELECT c.City FROM #C.entities() c)
              AND a.Population > 100";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PRAGUE", "CZECH", 50)
                ]
            },
            {
                "#B", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PRAGUE", "CZECH", 200)
                ]
            },
            {
                "#C", [new BasicEntity("BERLIN", "GERMANY", 250)]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Only WARSAW: PRAGUE excluded by Population filter, BERLIN excluded by EXCEPT");
        Assert.AreEqual("WARSAW", (string)table[0].Values[0]);
    }
}
