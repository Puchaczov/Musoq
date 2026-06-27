using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CommonSubexpressionEliminationEdgeCaseTests
{
    #region Mixed Column and Method Context Tests

    [TestMethod]
    public void WhenColumnPassedToMethodMultipleTimes_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Inc(Population),
                Inc(Population) * 2,
                Inc(Population) + Inc(Population)
            FROM #A.Entities()
            WHERE Inc(Population) > 50";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 100),
                    new BasicEntity("USA", 40)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(101m, table[0][0]);
        Assert.AreEqual(202m, table[0][1]);
        Assert.AreEqual(202m, table[0][2]);
    }

    [TestMethod]
    public void WhenColumnUsedDirectlyAndPassedToMethod_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Population,
                Inc(Population),
                Population + Inc(Population),
                Population * Inc(Population)
            FROM #A.Entities()
            WHERE Population > 50 AND Inc(Population) > 100";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 100),
                    new BasicEntity("USA", 50),
                    new BasicEntity("UK", 60)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100m, table[0][0]);
        Assert.AreEqual(101m, table[0][1]);
        Assert.AreEqual(201m, table[0][2]);
        Assert.AreEqual(10100m, table[0][3]);
    }

    [TestMethod]
    public void WhenMultipleColumnsPassedToSameMethod_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Inc(Population),
                Inc(Population) + 10,
                Money,
                Inc(Money)
            FROM #A.Entities()
            WHERE Inc(Population) > 0";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "Poland", Population = 100, Money = 50 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(101m, table[0][0]);
        Assert.AreEqual(111m, table[0][1]);
        Assert.AreEqual(50m, table[0][2]);
        Assert.AreEqual(51m, table[0][3]);
    }

    [TestMethod]
    public void WhenColumnInCaseWhenWithMethodCalls_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Population,
                Inc(Population),
                CASE
                    WHEN Population > 150 AND Inc(Population) > 200 THEN 'High'
                    WHEN Population > 50 THEN 'Medium'
                    ELSE 'Low'
                END as Category,
                Inc(Population) * 2
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 200),
                    new BasicEntity("USA", 100),
                    new BasicEntity("UK", 30)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var rows = table.OrderBy(r => (decimal)r[0]).ToList();
        Assert.AreEqual(30m, rows[0][0]);
        Assert.AreEqual(31m, rows[0][1]);
        Assert.AreEqual("Low", rows[0][2]);
        Assert.AreEqual(62m, rows[0][3]);

        Assert.AreEqual(100m, rows[1][0]);
        Assert.AreEqual(101m, rows[1][1]);
        Assert.AreEqual("Medium", rows[1][2]);
        Assert.AreEqual(202m, rows[1][3]);

        Assert.AreEqual(200m, rows[2][0]);
        Assert.AreEqual(201m, rows[2][1]);
        Assert.AreEqual("High", rows[2][2]);
        Assert.AreEqual(402m, rows[2][3]);
    }

    [TestMethod]
    public void WhenNestedMethodCallsWithColumn_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Population,
                Inc(Inc(Population)),
                Inc(Inc(Population)) + Population
            FROM #A.Entities()
            WHERE Inc(Inc(Population)) > 100";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100m, table[0][0]);
        Assert.AreEqual(102m, table[0][1]);
        Assert.AreEqual(202m, table[0][2]);
    }

    [TestMethod]
    public void WhenColumnUsedInComplexArithmeticWithMethods_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Population,
                Inc(Population),
                (Population + Inc(Population)) * 2,
                Population * Population + Inc(Population) * Inc(Population)
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 10)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(10m, table[0][0]);
        Assert.AreEqual(11m, table[0][1]);
        Assert.AreEqual(42m, table[0][2]);
        Assert.AreEqual(221m, table[0][3]);
    }

    [TestMethod]
    public void WhenMultipleColumnsWithMultipleMethods_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Country,
                Population,
                Concat(Country, '_test'),
                Inc(Population),
                Concat(Country, '_suffix'),
                Inc(Population) + Population
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Poland", table[0][0]);
        Assert.AreEqual(100m, table[0][1]);
        Assert.AreEqual("Poland_test", table[0][2]);
        Assert.AreEqual(101m, table[0][3]);
        Assert.AreEqual("Poland_suffix", table[0][4]);
        Assert.AreEqual(201m, table[0][5]);
    }

    [TestMethod]
    public void WhenColumnUsedInWhereSelectAndOrderBy_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Population,
                Inc(Population),
                Population * 2
            FROM #A.Entities()
            WHERE Population > 50 AND Inc(Population) < 200
            ORDER BY Population DESC";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 100),
                    new BasicEntity("USA", 150),
                    new BasicEntity("UK", 50),
                    new BasicEntity("France", 250)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);


        Assert.AreEqual(150m, table[0][0]);
        Assert.AreEqual(151m, table[0][1]);
        Assert.AreEqual(300m, table[0][2]);

        Assert.AreEqual(100m, table[1][0]);
        Assert.AreEqual(101m, table[1][1]);
        Assert.AreEqual(200m, table[1][2]);
    }

    [TestMethod]
    public void WhenSameColumnPassedToDifferentMethods_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Population,
                Inc(Population),
                ToString(Population),
                Abs(Population - 150)
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 100),
                    new BasicEntity("USA", 200)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (decimal)r[0]).ToList();
        Assert.AreEqual(100m, rows[0][0]);
        Assert.AreEqual(101m, rows[0][1]);
        Assert.AreEqual("100", rows[0][2]);
        Assert.AreEqual(50m, rows[0][3]);

        Assert.AreEqual(200m, rows[1][0]);
        Assert.AreEqual(201m, rows[1][1]);
        Assert.AreEqual("200", rows[1][2]);
        Assert.AreEqual(50m, rows[1][3]);
    }

    [TestMethod]
    public void WhenColumnExpressionPassedToMethod_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Population,
                Population + 10,
                Inc(Population + 10),
                Inc(Population + 10) * 2
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100m, table[0][0]);
        Assert.AreEqual(110m, table[0][1]);
        Assert.AreEqual(111m, table[0][2]);
        Assert.AreEqual(222m, table[0][3]);
    }

    [TestMethod]
    public void WhenMixedColumnMethodAndLiterals_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Population,
                Inc(Population),
                Population + 100 + Inc(Population) + 50,
                (Population * 2) + (Inc(Population) * 3) + 1000
            FROM #A.Entities()
            WHERE Population + Inc(Population) > 100";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100m, table[0][0]);
        Assert.AreEqual(101m, table[0][1]);
        Assert.AreEqual(351m, table[0][2]);
        Assert.AreEqual(1503m, table[0][3]);
    }

    #endregion
}
