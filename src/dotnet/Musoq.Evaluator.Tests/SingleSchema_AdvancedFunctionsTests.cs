#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Single schema tests: Advanced functions, DateTime, CASE/WHEN, and comments.
/// </summary>
[TestClass]
public class SingleSchema_AdvancedFunctionsTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }
    [TestMethod]
    public void MatchWithRegexTest()
    {
        var query = @"select Match('\d{7}', Name) from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("3213213")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsTrue((bool?)table[0][0]);
    }

    [TestMethod]
    public void HeadWithStringTest()
    {
        var query = "select Head('ABCDEF', 2) from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("3213213")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual("AB", table[0][0]);
    }

    [TestMethod]
    public void TailWithStringTest()
    {
        var query = "select Tail('ABCDEF', 2) from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("3213213")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual("EF", table[0][0]);
    }

    [TestMethod]
    public void SubtractTwoAliasedValuesTest()
    {
        var query = "select a.Money - a.Money from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("may", 2512m)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0m, table[0][0]);
    }

    [TestMethod]
    public void SubtractThreeAliasedValuesTest()
    {
        var query = "select (a.Money - a.Population) / a.Money from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("may", 100m) { Population = 10 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0.9m, table[0][0]);
    }

    [TestMethod]
    public void FilterByComplexObjectAccessInWhereTest()
    {
        var query = "select Population from #A.entities() where Self.Money > 100";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("may", 100m) { Population = 10 },
                    new BasicEntity("june", 200m) { Population = 20 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(20m, table[0][0]);
    }

    [TestMethod]
    public void ComputeStDevTest()
    {
        var query = "select StDev(Population) from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("may", 100m) { Population = 10 },
                    new BasicEntity("june", 200m) { Population = 20 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsGreaterThan((decimal)table[0][0] - 7.071m, 0.001m);
    }

    [TestMethod]
    public void CaseWhenSimpleTest()
    {
        var query = "select " +
                    "   (case " +
                    "       when Population > 100d" +
                    "       then true" +
                    "       else false" +
                    "   end)" +
                    "from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("may", 100m) { Population = 100 },
                    new BasicEntity("june", 200m) { Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => !(bool)entry[0]), "First entry should be false");
        Assert.IsTrue(table.Any(entry => (bool)entry[0]), "Second entry should be true");
    }

    [TestMethod]
    public void CaseWhenWithLibraryMethodCallTest()
    {
        var query = "select " +
                    "   (case " +
                    "       when Population > 100d" +
                    "       then entities.GetOne()" +
                    "       else entities.Inc(entities.GetOne())" +
                    "   end)" +
                    "from #A.entities() entities";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("may", 100m) { Population = 100 },
                    new BasicEntity("june", 200m) { Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row => (decimal)row[0] == 2m) &&
                      table.Any(row => (decimal)row[0] == 1m),
            "Expected rows with values 2 and 1");
    }

    [TestMethod]
    public void MultipleCaseWhenWithLibraryMethodCallTest()
    {
        var query = "select " +
                    "   (case " +
                    "       when Population > 100d" +
                    "       then entities.GetOne()" +
                    "       else entities.Inc(entities.GetOne())" +
                    "   end)," +
                    "   (case " +
                    "       when Population <= 100d" +
                    "       then entities.GetOne()" +
                    "       else entities.Inc(entities.GetOne())" +
                    "   end)" +
                    "from #A.entities() entities";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("may", 100m) { Population = 100 },
                    new BasicEntity("june", 200m) { Population = 200 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Table should contain 2 rows");

        Assert.IsTrue(table.Any(row =>
                (decimal)row[0] == 2m &&
                (decimal)row[1] == 1m),
            "Row with values (2,1) not found");

        Assert.IsTrue(table.Any(row =>
                (decimal)row[0] == 1m &&
                (decimal)row[1] == 2m),
            "Row with values (1,2) not found");
    }

    [TestMethod]
    public void QueryWithTimeSpanTest()
    {
        var query = "select ToTimeSpan('00:12:15') from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("may", 100m) { Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(new TimeSpan(0, 12, 15), table[0][0]);
    }

    [TestMethod]
    public void QueryWithToDateTimeTest()
    {
        var query = "select ToDateTime('2012/01/13') from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("may", 100m) { Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(new DateTime(2012, 1, 13), table[0][0]);
    }

    [TestMethod]
    public void QueryWithToDateTimeAndTimeSpanAdditionTest()
    {
        var query = "select ToDateTime('2012/01/13') + ToTimeSpan('00:12:15') from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("may", 100m) { Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(new DateTime(2012, 1, 13, 0, 12, 15), table[0][0]);
    }

    [TestMethod]
    public void QueryWithTimeSpansAdditionTest()
    {
        var query = "select ToTimeSpan('00:12:15') + ToTimeSpan('00:12:15') from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("may", 100m) { Population = 100 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(new TimeSpan(0, 24, 30), table[0][0]);
    }

    [TestMethod]
    public void RegexMatchesIntegrationTest()
    {
        var query = @"select RegexMatches('\d+', Name) from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Test 123 and 456")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(typeof(string[]), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(1, table.Count);
        var result = (string[])table[0].Values[0];
        Assert.HasCount(2, result);
        Assert.AreEqual("123", result[0]);
        Assert.AreEqual("456", result[1]);
    }

    [TestMethod]
    public void WhenTwoCommentsWithEmptyLineThenQuery_ShouldEvaluate()
    {
        var query = """
                    --comment 1
                    --comment 2

                    select Name from #A.Entities()
                    """;
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("Test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test", table[0][0]);
    }

    [TestMethod]
    public void WhenMultipleCommentsWithEmptyLinesThenQuery_ShouldEvaluate()
    {
        var query = """
                    --comment 1
                    --comment 2
                    --comment 3


                    select Name from #A.Entities() where Name = 'Test'
                    """;
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("Test"), new BasicEntity("Other")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test", table[0][0]);
    }

    [TestMethod]
    public void WhenMultiLineCommentWithEmptyLineThenQuery_ShouldEvaluate()
    {
        var query = """
                    /* multi-line comment
                       spanning multiple lines */

                    select Name from #A.Entities()
                    """;
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("Test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test", table[0][0]);
    }

    [TestMethod]
    public void WhenMixedCommentsWithEmptyLinesThenQuery_ShouldEvaluate()
    {
        var query = """
                    --single line comment
                    /* multi-line
                       comment */

                    select Name from #A.Entities()
                    """;
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("Test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Test", table[0][0]);
    }
}
