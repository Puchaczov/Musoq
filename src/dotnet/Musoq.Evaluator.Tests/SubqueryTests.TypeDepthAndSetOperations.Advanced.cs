using System;
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
    public void WhenInSubquery_WithExistingJoinAndMultipleInClauses_ShouldWork()
    {
        var query = @"
            SELECT a.City, b.City FROM #A.entities() a
            INNER JOIN #B.entities() b ON a.Country = b.Country
            WHERE a.City IN (SELECT c.City FROM #C.entities() c)
              AND b.Population IN (SELECT d.Population FROM #D.entities() d)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
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
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
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
        Assert.AreEqual("KRAKOW", (string)table[0].Values[1]);
    }

    // ── MEDIUM PRIORITY: Subquery with ORDER BY ───────────────────────────────

    [TestMethod]
    public void WhenInSubquery_SubqueryHasOrderBy_ShouldNotError()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b ORDER BY b.City DESC)";

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
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "WARSAW"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "BERLIN"));
    }

    // ── MEDIUM PRIORITY: Subquery with SKIP ───────────────────────────────────

    [TestMethod]
    public void WhenInSubquery_SubqueryHasSkip_ShouldLimitSubqueryResults()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (
                SELECT b.City FROM #B.entities() b
                ORDER BY b.City ASC
                SKIP 1
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
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "PARIS"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "WARSAW"));
    }

    // ── MEDIUM PRIORITY: Implicit type conversion ─────────────────────────────

    [TestMethod]
    public void WhenInSubquery_WithNullableIntVsDecimal_ShouldWork()
    {
        var query = @"
            SELECT a.Name FROM #A.entities() a
            WHERE a.NullableValue IN (SELECT b.Id FROM #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "MATCH", NullableValue = 1 },
                    new BasicEntity { Name = "NO_MATCH", NullableValue = 99 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "X", Id = 1 },
                    new BasicEntity { Name = "Y", Id = 2 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("MATCH", (string)table[0].Values[0]);
    }

    // ── MEDIUM PRIORITY: Subquery with DISTINCT already present ───────────────

    [TestMethod]
    public void WhenInSubquery_SubqueryAlreadyHasDistinct_ShouldWork()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT DISTINCT b.City FROM #B.entities() b)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            },
            {
                "#B", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("WARSAW", "POLAND", 400),
                    new BasicEntity("WARSAW", "POLAND", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("WARSAW", (string)table[0].Values[0]);
    }

    // ── Error quality: Explanation and SuggestedFixes ──────────────────────────

    [TestMethod]
    public void WhenInSubquery_WithUnionAll_ShouldIncludeEntriesFromBothSides()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.City FROM #B.entities() b UNION ALL (City) SELECT c.City FROM #C.entities() c)";

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

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "WARSAW"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "BERLIN"));
    }

    [TestMethod]
    public void WhenInSubquery_MultipleColumnsError_ShouldHaveExplanationAndFixes()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN (SELECT b.City, b.Country FROM #B.entities() b)";

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

        Assert.AreEqual(DiagnosticCode.MQ3049_InSubqueryMultipleColumns, ex.PrimaryEnvelope.Code);
        Assert.IsNotNull(ex.PrimaryEnvelope.Explanation, "Explanation should be populated.");
        Assert.IsNotEmpty(ex.PrimaryEnvelope.SuggestedFixes, "SuggestedFixes should contain at least one entry.");
        Assert.IsTrue(
            ex.PrimaryEnvelope.SuggestedFixes.Any(f => f.Contains("single column", StringComparison.OrdinalIgnoreCase)),
            "SuggestedFixes should mention reducing to a single column.");
    }
}
