using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Integration tests verifying that non-deterministic methods (marked with [NonDeterministic])
///     behave correctly in queries and are NOT cached by Common Subexpression Elimination (CSE).
/// </summary>
[global::Musoq.Tests.Common.CultureTestClass]
public class NonDeterministicMethodsQueryTests : BasicEntityTestBase
{
    #region Non-Deterministic in WHERE and SELECT

    [TestMethod]
    public void WhenNonDeterministicMethodInWhereAndSelect_ShouldNotBeCached()
    {
        const string query =
            @"
            SELECT NextTestValue() as RandomVal, Name
            FROM #A.Entities()
            WHERE NextTestValue() >= 0";
        var fixture = new EvaluationFixture("A", "B", "C");
        using var compiled = CreateAndRunVirtualMachine(query, schemaProvider: fixture.Provider);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [2, "A"], [12, "B"], [22, "C"]);
        Assert.AreEqual(6, fixture.Calls);
    }

    #endregion

    #region Non-Deterministic with CASE WHEN

    [TestMethod]
    public void WhenNonDeterministicMethodInCaseWhen_ShouldWork()
    {
        const string query =
            @"
            SELECT
                CASE WHEN NextTestValue() < 50 THEN 'Low' ELSE 'High' END as Category,
                Name
            FROM #A.Entities()";
        var fixture = new EvaluationFixture("A", "B", "C", "D");
        using var compiled = CreateAndRunVirtualMachine(query, schemaProvider: fixture.Provider);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Low", "A"],
            ["Low", "B"],
            ["Low", "C"],
            ["Low", "D"]
        );
        Assert.AreEqual(4, fixture.Calls);
    }

    #endregion

    #region Non-Deterministic in Various Contexts

    [TestMethod]
    public void WhenNonDeterministicMethodInSubquery_ShouldWork()
    {
        const string query =
            @"
            SELECT Name, NextTestValue() as Rand
            FROM #A.Entities()
            WHERE Name IN (SELECT Name FROM #A.Entities() WHERE Name IN ('A', 'B'))";
        var fixture = new EvaluationFixture("A", "B", "C", "D");
        using var compiled = CreateAndRunVirtualMachine(query, schemaProvider: fixture.Provider);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A", 1], ["B", 11]);
        Assert.AreEqual(2, fixture.Calls);
    }

    #endregion

    #region Basic Non-Deterministic Method Tests

    [TestMethod]
    public void WhenNonDeterministicMethodInSelect_ShouldReturnDifferentValuesPerRow()
    {
        const string query = "SELECT NextTestValue(), Name FROM #A.Entities()";
        var fixture = new EvaluationFixture("A", "B", "C", "D", "E");
        using var compiled = CreateAndRunVirtualMachine(query, schemaProvider: fixture.Provider);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [1, "A"], [11, "B"], [21, "C"], [31, "D"], [41, "E"]);
        Assert.AreEqual(5, fixture.Calls);
    }

    [TestMethod]
    public void WhenNonDeterministicMethodAppearsMultipleTimes_ShouldNotBeCached()
    {
        const string query = "SELECT NextTestValue() as R1, NextTestValue() as R2, Name FROM #A.Entities()";
        var fixture = new EvaluationFixture("Test1", "Test2", "Test3");
        using var compiled = CreateAndRunVirtualMachine(query, schemaProvider: fixture.Provider);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [1, 2, "Test1"],
            [11, 12, "Test2"],
            [21, 22, "Test3"]
        );
        Assert.AreEqual(6, fixture.Calls);
    }

    #endregion

    #region Non-Deterministic with Arithmetic

    [TestMethod]
    public void WhenNonDeterministicMethodInArithmetic_ShouldWork()
    {
        const string query = "SELECT NextTestValue() + 100 as Shifted, Name FROM #A.Entities()";
        var fixture = new EvaluationFixture("A", "B");
        using var compiled = CreateAndRunVirtualMachine(query, schemaProvider: fixture.Provider);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [101, "A"], [111, "B"]);
        Assert.AreEqual(2, fixture.Calls);
    }

    [TestMethod]
    public void WhenNonDeterministicMethodInMultipleArithmeticExpressions_ShouldNotBeCached()
    {
        const string query =
            @"
            SELECT
                NextTestValue() + 100 as Shifted1,
                NextTestValue() * 2 as Doubled,
                Name
            FROM #A.Entities()";
        var fixture = new EvaluationFixture("A", "B");
        using var compiled = CreateAndRunVirtualMachine(query, schemaProvider: fixture.Provider);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [101, 4, "A"], [111, 24, "B"]);
        Assert.AreEqual(4, fixture.Calls);
    }

    #endregion

    #region Deterministic vs Non-Deterministic Comparison

    [TestMethod]
    public void WhenDeterministicMethodAppearsMultipleTimes_ShouldReturnExpectedValues()
    {
        const string query = "SELECT Length(Name) as Len1, Length(Name) as Len2, Name FROM #A.Entities()";
        var fixture = new EvaluationFixture("ABC", "DEFGH");
        using var compiled = CreateAndRunVirtualMachine(query, schemaProvider: fixture.Provider);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [3, 3, "ABC"], [5, 5, "DEFGH"]);
        Assert.AreEqual(0, fixture.Calls);
    }

    [TestMethod]
    public void WhenMixingDeterministicAndNonDeterministic_ShouldHandleCorrectly()
    {
        const string query =
            @"
            SELECT
                Length(Name) as NameLength,
                Length(Name) + 10 as LengthPlus10,
                NextTestValue() as Random1,
                NextTestValue() as Random2,
                Name
            FROM #A.Entities()";
        var fixture = new EvaluationFixture("Test", "Hello");
        using var compiled = CreateAndRunVirtualMachine(query, schemaProvider: fixture.Provider);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [4, 14, 1, 2, "Test"], [5, 15, 11, 12, "Hello"]);
        Assert.AreEqual(4, fixture.Calls);
    }

    #endregion

    #region Non-Deterministic with GROUP BY

    [TestMethod]
    public void WhenNonDeterministicMethodWithGroupBy_ShouldWork()
    {
        const string query =
            @"
            SELECT
                Name,
                Count(Name) as Cnt, NextGroupValue() as Probe
            FROM #A.Entities()
            GROUP BY Name
            HAVING Count(Name) > 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A"),
                    new BasicEntity("A"),
                    new BasicEntity("B"),
                    new BasicEntity("B"),
                    new BasicEntity("B"),
                    new BasicEntity("C"),
                ]
            },
        };

        var fixture = new EvaluationFixture(sources["#A"].Select(entity => entity.Name!).ToArray());
        var vm = CreateAndRunVirtualMachine(query, schemaProvider: fixture.Provider);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(0, fixture.Calls);
        CollectionAssert.AreEquivalent(
            new[] { 1, 2 },
            table.Select(row => Convert.ToInt32(row.ReadValue(2))).ToArray()
        );

        var rowA = table.First(r => (string)r.ReadValue(0) == "A");
        var rowB = table.First(r => (string)r.ReadValue(0) == "B");

        Assert.AreEqual(2, Convert.ToInt32(rowA.ReadValue(1)));
        Assert.AreEqual(3, Convert.ToInt32(rowB.ReadValue(1)));
    }

    [TestMethod]
    public void WhenNonDeterministicMethodInHaving_ShouldNotBeCached()
    {
        const string query =
            @"
            SELECT
                Name,
                Count(Name) as Cnt
            FROM #A.Entities()
            GROUP BY Name
            HAVING NextGroupValue() = 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A"), new BasicEntity("A"), new BasicEntity("B")] },
        };

        var fixture = new EvaluationFixture(sources["#A"].Select(entity => entity.Name!).ToArray());
        var vm = CreateAndRunVirtualMachine(query, schemaProvider: fixture.Provider);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0, fixture.Calls);
        var expectedCounts = new Dictionary<string, int> { ["A"] = 2, ["B"] = 1 };
        Assert.AreEqual(expectedCounts[(string)table[0].ReadValue(0)], Convert.ToInt32(table[0].ReadValue(1)));
    }

    #endregion
}
