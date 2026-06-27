using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CommonSubexpressionEliminationEdgeCaseTests
{
    #region CASE WHEN Edge Cases

    [TestMethod]
    public void WhenNestedCallInCaseWhenCondition_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                ToString(Population) as Pop,
                CASE WHEN Length(ToString(Population)) > 2 THEN 'Long' ELSE 'Short' END as Category
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 10),
                    new BasicEntity("B", 100),
                    new BasicEntity("C", 1000)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var row10 = table.First(r => (string)r[0] == "10");
        Assert.AreEqual("Short", row10[1]);

        var row100 = table.First(r => (string)r[0] == "100");
        Assert.AreEqual("Long", row100[1]);

        var row1000 = table.First(r => (string)r[0] == "1000");
        Assert.AreEqual("Long", row1000[1]);
    }

    [TestMethod]
    public void WhenSameExpressionInMultipleCaseWhenBranches_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                CASE
                    WHEN Length(Name) > 4 THEN 'Very Long'
                    WHEN Length(Name) > 2 THEN 'Long'
                    WHEN Length(Name) > 0 THEN 'Short'
                    ELSE 'Empty'
                END as Category
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A"),
                    new BasicEntity("ABC"),
                    new BasicEntity("ABCDEF")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Short"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Long"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Very Long"));
    }

    [TestMethod]
    public void WhenCaseWhenInWhereClause_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT Name, Length(Name) as Len
            FROM #A.Entities()
            WHERE (CASE WHEN Length(Name) > 2 THEN 1 ELSE 0 END) = 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("AB"),
                    new BasicEntity("ABC"),
                    new BasicEntity("ABCDE")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.All(row => Convert.ToInt32(row[1]) > 2));
    }

    [TestMethod]
    public void WhenNestedCaseWhenExpressions_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                CASE
                    WHEN Length(Name) > 3 THEN
                        CASE
                            WHEN Length(Name) > 5 THEN 'Very Long'
                            ELSE 'Long'
                        END
                    ELSE 'Short'
                END as Category
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("AB"),
                    new BasicEntity("ABCD"),
                    new BasicEntity("ABCDEFG")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Short"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Long"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Very Long"));
    }

    #endregion

    #region Short-Circuit Evaluation Correctness

    [TestMethod]
    public void WhenExpressionInShortCircuitedBranch_ShouldNotCauseIssues()
    {
        const string query = @"
            SELECT Name
            FROM #A.Entities()
            WHERE Name IS NOT NULL AND Length(Name) > 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity(null),
                    new BasicEntity("AB"),
                    new BasicEntity("ABCD")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("ABCD", (string)table[0][0]);
    }

    [TestMethod]
    public void WhenOrConditionWithSharedExpression_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT Name, Length(Name) as Len
            FROM #A.Entities()
            WHERE Length(Name) = 2 OR Length(Name) = 5";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("AB"),
                    new BasicEntity("ABC"),
                    new BasicEntity("ABCDE")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row[0] == "AB"));
        Assert.IsTrue(table.Any(row => (string)row[0] == "ABCDE"));
    }

    #endregion

    #region NULL Handling Edge Cases

    [TestMethod]
    public void WhenNullableExpressionWithMultipleUses_ShouldHandleCorrectly()
    {
        const string query = @"
            SELECT
                NullableValue,
                CASE WHEN NullableValue IS NULL THEN 0 ELSE NullableValue END as SafeValue
            FROM #A.Entities()
            WHERE NullableValue IS NULL OR NullableValue > 5";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "A", NullableValue = null },
                    new BasicEntity { Name = "B", NullableValue = 3 },
                    new BasicEntity { Name = "C", NullableValue = 10 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var nullRow = table.First(r => r[0] == null);
        Assert.AreEqual(0, Convert.ToInt32(nullRow[1]));

        var valueRow = table.First(r => r[0] != null);
        Assert.AreEqual(10, Convert.ToInt32(valueRow[1]));
    }

    [TestMethod]
    public void WhenCoalesceWithSharedExpression_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT
                Coalesce(NullableValue, 0) as Safe,
                NullableValue
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "A", NullableValue = null },
                    new BasicEntity { Name = "B", NullableValue = 42 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var nullRow = table.First(r => r[1] == null);
        Assert.AreEqual(0, Convert.ToInt32(nullRow[0]));

        var valueRow = table.First(r => r[1] != null);
        Assert.AreEqual(42, Convert.ToInt32(valueRow[0]));
    }

    #endregion

    #region Expression Appearing in Different Clause Types

    [TestMethod]
    public void WhenExpressionInSelectWhereAndOrderBy_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT Name, Length(Name) as Len
            FROM #A.Entities()
            WHERE Length(Name) >= 2
            ORDER BY Length(Name) DESC";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A"),
                    new BasicEntity("AB"),
                    new BasicEntity("ABCDE")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.AreEqual(5, Convert.ToInt32(table[0][1]));
        Assert.AreEqual(2, Convert.ToInt32(table[1][1]));
    }

    [TestMethod]
    public void WhenExpressionInGroupByAndHaving_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT Length(Country) as CountryLen, Count(Country) as Cnt
            FROM #A.Entities()
            GROUP BY Length(Country)
            HAVING Count(Country) > 1";


        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("USA", 100),
                    new BasicEntity("Poland", 200),
                    new BasicEntity("Germany", 300),
                    new BasicEntity("Poland", 400),
                    new BasicEntity("UK", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(1, table.Count);
        Assert.IsTrue(table.Any(row => Convert.ToInt32(row[0]) == 6 && Convert.ToInt32(row[1]) == 2));
    }

    #endregion
}
