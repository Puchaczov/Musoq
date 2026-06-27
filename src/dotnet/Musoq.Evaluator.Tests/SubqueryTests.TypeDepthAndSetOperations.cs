using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    // ── HIGH PRIORITY: Type handling ──────────────────────────────────────────

    [TestMethod]
    public void WhenInSubquery_WithStringVsNumericMismatch_ShouldError()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.Population FROM #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("WARSAW", "POLAND", 500)]
            },
            {
                "#B", [new BasicEntity("WARSAW", "POLAND", 500)]
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() =>
        {
            var vm = CreateAndRunVirtualMachine(query, sources);
            vm.Run(TestContext.CancellationToken);
        });

        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, ex.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void WhenInSubquery_ThreeLevelsDeep_ShouldWork()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.Country IN (
                SELECT b.Country FROM #B.entities() b
                WHERE b.City IN (
                    SELECT c.City FROM #C.entities() c
                    WHERE c.Population IN (SELECT d.Population FROM #D.entities() d)
                )
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
                    new BasicEntity("KRAKOW", "POLAND", 400),
                    new BasicEntity("MUNICH", "GERMANY", 200)
                ]
            },
            {
                "#D", [
                    new BasicEntity("X", "Y", 400)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", (string)table[0].Values[0]);
    }

    // ── MEDIUM PRIORITY: Aggregate in subquery ────────────────────────────────

    [TestMethod]
    public void WhenInSubquery_SubqueryReturnsAggregateResult_ShouldWork()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.Population IN (SELECT Max(b.Population) FROM #B.entities() b)";

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
                    new BasicEntity("X", "Y", 300),
                    new BasicEntity("X", "Y", 500),
                    new BasicEntity("X", "Y", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", (string)table[0].Values[0]);
    }

    // ── MEDIUM PRIORITY: EXCEPT/INTERSECT error messages ──────────────────────

    [TestMethod]
    public void WhenInSubquery_WithExceptInSubquery_ShouldWork()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b EXCEPT (City) SELECT c.City FROM #C.entities() c)";

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
                "#B", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            },
            {
                "#C", [new BasicEntity("BERLIN", "GERMANY", 250)]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Only WARSAW should remain after EXCEPT removes BERLIN");
        Assert.AreEqual("WARSAW", (string)table[0].Values[0]);
    }

    [TestMethod]
    public void WhenInSubquery_WithIntersectInSubquery_ShouldWork()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b INTERSECT (City) SELECT c.City FROM #C.entities() c)";

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
                "#B", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            },
            {
                "#C", [
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PRAGUE", "CZECH", 200)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Only BERLIN should remain from the intersection");
        Assert.AreEqual("BERLIN", (string)table[0].Values[0]);
    }

    // ── MEDIUM PRIORITY: OR at lower level (non-IN branches) ──────────────────

    [TestMethod]
    public void WhenInSubquery_WithOrInSeparateAndBranch_ShouldWork()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.Population IN (SELECT b.Population FROM #B.entities() b)
              AND (a.Country = 'GERMANY' OR a.Country = 'FRANCE')";

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
                    new BasicEntity("X", "Y", 250),
                    new BasicEntity("X", "Y", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "BERLIN"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "PARIS"));
    }

    // ── MEDIUM PRIORITY: Complex JOIN + multi-IN ──────────────────────────────

}
