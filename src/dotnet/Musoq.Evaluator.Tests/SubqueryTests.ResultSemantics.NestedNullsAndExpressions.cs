using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenNotInSubquery_NestedInSubquery_ShouldWork()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.Country NOT IN (
                SELECT b.Country FROM #B.entities() b
                WHERE b.City IN (SELECT c.City FROM #C.entities() c)
            )";

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
                    new BasicEntity("KRAKOW", "POLAND", 400),
                    new BasicEntity("MUNICH", "GERMANY", 200)
                ]
            },
            {
                "#C", [
                    new BasicEntity("KRAKOW", "POLAND", 400)
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
    public void WhenInSubquery_WithUnqualifiedSumAggregate_ShouldWork()
    {
        var query = @"
            SELECT a.Country, Sum(a.Population) FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b)
            GROUP BY a.Country";

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
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "POLAND" && (decimal)r.Values[1] == 900m));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "GERMANY" && (decimal)r.Values[1] == 250m));
    }

    // ── HIGH PRIORITY: NULL handling ───────────────────────────────────────────

    [TestMethod]
    public void WhenInSubquery_WithNullableColumnInNonJoinPosition_ShouldWork()
    {
        var query = @"
            SELECT a.Name, a.NullableValue FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "HAS_VALUE", City = "WARSAW", NullableValue = 10 },
                    new BasicEntity { Name = "IS_NULL", City = "BERLIN", NullableValue = null },
                    new BasicEntity { Name = "NO_MATCH", City = "PARIS", NullableValue = 30 }
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
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "HAS_VALUE" && (int?)r.Values[1] == 10));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "IS_NULL" && r.Values[1] == null));
    }

    [TestMethod]
    public void WhenInSubquery_SubqueryResultHasNull_ShouldFilterNullsInSubquery()
    {
        var query = @"
            SELECT a.Name FROM #A.entities() a
            WHERE a.NullableValue IN (
                SELECT b.NullableValue FROM #B.entities() b
                WHERE b.NullableValue IS NOT NULL
            )";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "VAL_10", NullableValue = 10 },
                    new BasicEntity { Name = "VAL_30", NullableValue = 30 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "X", NullableValue = 10 },
                    new BasicEntity { Name = "Y", NullableValue = null }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("VAL_10", (string)table[0].Values[0]);
    }

    [TestMethod]
    public void WhenNotInSubquery_SubqueryResultHasNull_ShouldHandleCorrectly()
    {
        var query = @"
            SELECT a.Name FROM #A.entities() a
            WHERE a.NullableValue NOT IN (SELECT b.NullableValue FROM #B.entities() b WHERE b.NullableValue IS NOT NULL)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "VAL_10", NullableValue = 10 },
                    new BasicEntity { Name = "VAL_20", NullableValue = 20 },
                    new BasicEntity { Name = "VAL_30", NullableValue = 30 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "X", NullableValue = 10 },
                    new BasicEntity { Name = "Y", NullableValue = 20 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("VAL_30", (string)table[0].Values[0]);
    }

    // ── HIGH PRIORITY: Function call as left operand ──────────────────────────

    [TestMethod]
    public void WhenInSubquery_WithFunctionCallOnLeftSide_ShouldWork()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE ToUpper(a.City) IN (SELECT b.City FROM #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("warsaw", "POLAND", 500),
                    new BasicEntity("berlin", "GERMANY", 250),
                    new BasicEntity("paris", "FRANCE", 300)
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
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "warsaw"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "berlin"));
    }

    // ── HIGH PRIORITY: OR with IN subquery ─────────────────────────────────────

    [TestMethod]
    public void WhenInSubquery_WithOrCondition_ShouldReturnRowsMatchingEitherCondition()
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
                    new BasicEntity("PARIS", "FRANCE", 450)
                ]
            },
            {
                "#B", [
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "WARSAW"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "BERLIN"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "PARIS"));
    }
}
