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
        const string query = (
            "\n"
            + "            SELECT\n"
            + "                ToString(Population) as Pop,\n"
            + "                CASE WHEN Length(ToString(Population)) > 2 THEN 'Long' ELSE 'Short' END as Category\n"
            + "            FROM #A.Entities()"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A", 10), new BasicEntity("B", 100), new BasicEntity("C", 1000)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        var row10 = table.First(r => (string)r.ReadValue(0) == "10");
        Assert.AreEqual("Short", row10.ReadValue(1));

        var row100 = table.First(r => (string)r.ReadValue(0) == "100");
        Assert.AreEqual("Long", row100.ReadValue(1));

        var row1000 = table.First(r => (string)r.ReadValue(0) == "1000");
        Assert.AreEqual("Long", row1000.ReadValue(1));
    }

    [TestMethod]
    public void WhenSameExpressionInMultipleCaseWhenBranches_ShouldReturnCorrectResults()
    {
        const string query = (
            "\n"
            + "            SELECT\n"
            + "                CASE\n"
            + "                    WHEN Length(Name) > 4 THEN 'Very Long'\n"
            + "                    WHEN Length(Name) > 2 THEN 'Long'\n"
            + "                    WHEN Length(Name) > 0 THEN 'Short'\n"
            + "                    ELSE 'Empty'\n"
            + "                END as Category\n"
            + "            FROM #A.Entities()"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("ABC"), new BasicEntity("ABCDEF")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.ReadValues()[0] == "Short"));
        Assert.IsTrue(table.Any(row => (string)row.ReadValues()[0] == "Long"));
        Assert.IsTrue(table.Any(row => (string)row.ReadValues()[0] == "Very Long"));
    }

    [TestMethod]
    public void WhenCaseWhenInWhereClause_ShouldReturnCorrectResults()
    {
        const string query = (
            "\n"
            + "            SELECT Name, Length(Name) as Len\n"
            + "            FROM #A.Entities()\n"
            + "            WHERE (CASE WHEN Length(Name) > 2 THEN 1 ELSE 0 END) = 1"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("AB"), new BasicEntity("ABC"), new BasicEntity("ABCDE")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.All(row => Convert.ToInt32(row.ReadValue(1)) > 2));
    }

    [TestMethod]
    public void WhenNestedCaseWhenExpressions_ShouldReturnCorrectResults()
    {
        const string query = (
            "\n"
            + "            SELECT\n"
            + "                CASE\n"
            + "                    WHEN Length(Name) > 3 THEN\n"
            + "                        CASE\n"
            + "                            WHEN Length(Name) > 5 THEN 'Very Long'\n"
            + "                            ELSE 'Long'\n"
            + "                        END\n"
            + "                    ELSE 'Short'\n"
            + "                END as Category\n"
            + "            FROM #A.Entities()"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("AB"), new BasicEntity("ABCD"), new BasicEntity("ABCDEFG")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.ReadValues()[0] == "Short"));
        Assert.IsTrue(table.Any(row => (string)row.ReadValues()[0] == "Long"));
        Assert.IsTrue(table.Any(row => (string)row.ReadValues()[0] == "Very Long"));
    }

    #endregion

    #region Short-Circuit Evaluation Correctness

    [TestMethod]
    public void WhenExpressionInShortCircuitedBranch_ShouldNotCauseIssues()
    {
        const string query = (
            "\n"
            + "            SELECT Name\n"
            + "            FROM #A.Entities()\n"
            + "            WHERE Name IS NOT NULL AND Length(Name) > 2"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity(null), new BasicEntity("AB"), new BasicEntity("ABCD")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("ABCD", (string)table[0].ReadValue(0));
    }

    [TestMethod]
    public void WhenOrConditionWithSharedExpression_ShouldReturnCorrectResults()
    {
        const string query = (
            "\n"
            + "            SELECT Name, Length(Name) as Len\n"
            + "            FROM #A.Entities()\n"
            + "            WHERE Length(Name) = 2 OR Length(Name) = 5"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("AB"), new BasicEntity("ABC"), new BasicEntity("ABCDE")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.ReadValue(0) == "AB"));
        Assert.IsTrue(table.Any(row => (string)row.ReadValue(0) == "ABCDE"));
    }

    #endregion

    #region NULL Handling Edge Cases

    [TestMethod]
    public void WhenNullableExpressionWithMultipleUses_ShouldHandleCorrectly()
    {
        const string query = (
            "\n"
            + "            SELECT\n"
            + "                NullableValue,\n"
            + "                CASE WHEN NullableValue IS NULL THEN 0 ELSE NullableValue END as SafeValue\n"
            + "            FROM #A.Entities()\n"
            + "            WHERE NullableValue IS NULL OR NullableValue > 5"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "A", NullableValue = null },
                    new BasicEntity { Name = "B", NullableValue = 3 },
                    new BasicEntity { Name = "C", NullableValue = 10 },
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var nullRow = table.First(r => r.ReadValue(0) == null);
        Assert.AreEqual(0, Convert.ToInt32(nullRow.ReadValue(1)));

        var valueRow = table.First(r => r.ReadValue(0) != null);
        Assert.AreEqual(10, Convert.ToInt32(valueRow.ReadValue(1)));
    }

    [TestMethod]
    public void WhenCoalesceWithSharedExpression_ShouldReturnCorrectResults()
    {
        const string query = (
            "\n"
            + "            SELECT\n"
            + "                Coalesce(NullableValue, 0) as Safe,\n"
            + "                NullableValue\n"
            + "            FROM #A.Entities()"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "A", NullableValue = null },
                    new BasicEntity { Name = "B", NullableValue = 42 },
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var nullRow = table.First(r => r.ReadValue(1) == null);
        Assert.AreEqual(0, Convert.ToInt32(nullRow.ReadValue(0)));

        var valueRow = table.First(r => r.ReadValue(1) != null);
        Assert.AreEqual(42, Convert.ToInt32(valueRow.ReadValue(0)));
    }

    #endregion

    #region Expression Appearing in Different Clause Types

    [TestMethod]
    public void WhenExpressionInSelectWhereAndOrderBy_ShouldReturnCorrectResults()
    {
        const string query = (
            "\n"
            + "            SELECT Name, Length(Name) as Len\n"
            + "            FROM #A.Entities()\n"
            + "            WHERE Length(Name) >= 2\n"
            + "            ORDER BY Length(Name) DESC"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("AB"), new BasicEntity("ABCDE")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.AreEqual(5, Convert.ToInt32(table[0].ReadValue(1)));
        Assert.AreEqual(2, Convert.ToInt32(table[1].ReadValue(1)));
    }

    [TestMethod]
    public void WhenExpressionInGroupByAndHaving_ShouldReturnCorrectResults()
    {
        const string query = (
            "\n"
            + "            SELECT Length(Country) as CountryLen, Count(Country) as Cnt\n"
            + "            FROM #A.Entities()\n"
            + "            GROUP BY Length(Country)\n"
            + "            HAVING Count(Country) > 1"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("USA", 100),
                    new BasicEntity("Poland", 200),
                    new BasicEntity("Germany", 300),
                    new BasicEntity("Poland", 400),
                    new BasicEntity("UK", 500),
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.IsTrue(
            table.Any(row => Convert.ToInt32(row.ReadValue(0)) == 6 && Convert.ToInt32(row.ReadValue(1)) == 2)
        );
    }

    #endregion
}
