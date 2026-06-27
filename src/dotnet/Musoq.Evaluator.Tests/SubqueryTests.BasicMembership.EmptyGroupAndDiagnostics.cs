using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenNotInSubquery_EmptySubqueryResult_ShouldReturnAll()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City NOT IN (SELECT b.City FROM #B.entities() b WHERE b.Population > 9999)";

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
                    new BasicEntity("WARSAW", "POLAND", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "All rows should be returned when subquery is empty");
    }

    [TestMethod]
    public void WhenInSubquery_WithGroupByInSubquery_ShouldWork()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.Country IN (
                SELECT b.Country FROM #B.entities() b
                GROUP BY b.Country
                HAVING Count(b.Country) > 1
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
                    new BasicEntity("GDANSK", "POLAND", 300),
                    new BasicEntity("MUNICH", "GERMANY", 200)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Only POLAND has Count > 1 in #B");
        Assert.AreEqual("WARSAW", (string)table[0].Values[0]);
    }

    [TestMethod]
    public void WhenInSubquery_SameSource_ShouldWork()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.Country IN (
                SELECT b.Country FROM #A.entities() b WHERE b.Population > 400
            )";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("KRAKOW", "POLAND", 200),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "WARSAW and KRAKOW are in POLAND which has Population > 400 entry");
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "WARSAW"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "KRAKOW"));
    }

    [TestMethod]
    public void WhenInSubquery_LiteralList_StillWorks()
    {
        var query = @"
            SELECT a.City FROM #A.entities() a
            WHERE a.City IN ('WARSAW', 'BERLIN')";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("PARIS", "FRANCE", 300)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should still work with literal IN list");
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "WARSAW"));
        Assert.IsTrue(table.Any(r => (string)r.Values[0] == "BERLIN"));
    }

    [TestMethod]
    public void WhenInSubquery_MultipleColumns_ShouldError()
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
        Assert.IsTrue(
            ex.Message.Contains("one column") ||
            (ex.InnerException != null && ex.InnerException.Message.Contains("one column")),
            $"Expected message about single column, got: {ex.Message}");
    }
}
