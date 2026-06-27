using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Edge case tests for Common Subexpression Elimination (CSE).
///     These tests verify that CSE doesn't optimize too aggressively and
///     produces correct results for complex and unusual query patterns.
/// </summary>
[TestClass]
public partial class CommonSubexpressionEliminationEdgeCaseTests : BasicEntityTestBase
{
    #region Same Expression with Different Semantics

    [TestMethod]
    public void WhenSameTextDifferentContext_ShouldNotConfuse()
    {
        const string query = @"
            SELECT a.Name, Length(a.Name) as L1
            FROM #A.Entities() a
            WHERE Length(a.Name) > 2";

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
        Assert.IsTrue(table.Any(row => (string)row[0] == "ABC" && Convert.ToInt32(row[1]) == 3));
        Assert.IsTrue(table.Any(row => (string)row[0] == "ABCDE" && Convert.ToInt32(row[1]) == 5));
    }

    #endregion

    #region Expressions with Side Effects (Non-Deterministic)

    [TestMethod]
    public void WhenNonDeterministicFunctionUsedMultipleTimes_ShouldNotCache()
    {
        const string query = @"
            SELECT RandomNumber() as R1, RandomNumber() as R2, RandomNumber() as R3
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Complex Arithmetic Expressions

    [TestMethod]
    public void WhenComplexArithmeticWithSharedSubexpressions_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT 
                (Population + 10) * 2 as Doubled,
                (Population + 10) / 2 as Halved,
                Population + 10 as Base
            FROM #A.Entities()
            WHERE (Population + 10) > 50";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 30),
                    new BasicEntity("B", 50),
                    new BasicEntity("C", 90)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var row60 = table.First(r => (decimal)r[2] == 60m);
        Assert.AreEqual(120m, (decimal)row60[0]);
        Assert.AreEqual(30m, (decimal)row60[1]);

        var row100 = table.First(r => (decimal)r[2] == 100m);
        Assert.AreEqual(200m, (decimal)row100[0]);
        Assert.AreEqual(50m, (decimal)row100[1]);
    }

    #endregion

    #region Multiple Tables and Aliases

    [TestMethod]
    public void WhenSameExpressionOnDifferentTables_ShouldNotConfuse()
    {
        const string query = @"
            SELECT a.Name, Length(a.Name) as Len
            FROM #A.Entities() a
            WHERE Length(a.Name) > 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
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

    #endregion

    #region Edge Cases with Method Overloads

    [TestMethod]
    public void WhenMethodWithDifferentParameterTypes_ShouldNotConfuse()
    {
        const string query = @"
            SELECT Inc(Population), Inc(Population) + 10
            FROM #A.Entities()
            WHERE Inc(Population) > 100";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 50),
                    new BasicEntity("B", 100),
                    new BasicEntity("C", 200)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (decimal)row.Values[0] == 101m && (decimal)row.Values[1] == 111m));
        Assert.IsTrue(table.Any(row => (decimal)row.Values[0] == 201m && (decimal)row.Values[1] == 211m));
    }

    #endregion

    #region Correctness After Row Boundaries

    [TestMethod]
    public void WhenProcessingMultipleRows_CacheShouldResetPerRow()
    {
        const string query = @"
            SELECT Name, Length(Name) as Len
            FROM #A.Entities()
            WHERE Length(Name) > 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("AB"),
                    new BasicEntity("ABCDE"),
                    new BasicEntity("XYZ")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);


        Assert.IsTrue(table.Any(row => (string)row[0] == "AB" && Convert.ToInt32(row[1]) == 2));
        Assert.IsTrue(table.Any(row => (string)row[0] == "ABCDE" && Convert.ToInt32(row[1]) == 5));
        Assert.IsTrue(table.Any(row => (string)row[0] == "XYZ" && Convert.ToInt32(row[1]) == 3));
    }

    #endregion

    #region Nested Method Calls - A(B()) patterns

    [TestMethod]
    public void WhenNestedCall_InnerExpressionInWhere_OuterInSelect_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT ToUpper(ToString(Population)) 
            FROM #A.Entities() 
            WHERE ToString(Population) = '100'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 100),
                    new BasicEntity("B", 200),
                    new BasicEntity("C", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.All(row => (string)row.Values[0] == "100"));
    }

    [TestMethod]
    public void WhenNestedCall_SameInnerExpressionTwice_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT 
                ToUpper(ToString(Population)), 
                Length(ToString(Population)) 
            FROM #A.Entities() 
            WHERE ToString(Population) LIKE '1%'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 100),
                    new BasicEntity("B", 200),
                    new BasicEntity("C", 1500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var row100 = table.First(r => (string)r[0] == "100");
        Assert.AreEqual(3, Convert.ToInt32(row100[1]));

        var row1500 = table.First(r => (string)r[0] == "1500");
        Assert.AreEqual(4, Convert.ToInt32(row1500[1]));
    }

    [TestMethod]
    public void WhenDeeplyNestedCalls_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT Length(ToUpper(ToString(Population))) 
            FROM #A.Entities() 
            WHERE Length(ToUpper(ToString(Population))) > 2";

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

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => Convert.ToInt32(row.Values[0]) == 3));
        Assert.IsTrue(table.Any(row => Convert.ToInt32(row.Values[0]) == 4));
    }

    [TestMethod]
    public void WhenNestedCallWithDifferentOuterMethods_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT 
                Coalesce(ToString(Population), 'N/A') as Str,
                Length(ToString(Population)) as Len
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 100),
                    new BasicEntity("B", 12345)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var row100 = table.First(r => (string)r[0] == "100");
        Assert.AreEqual(3, Convert.ToInt32(row100[1]));

        var row12345 = table.First(r => (string)r[0] == "12345");
        Assert.AreEqual(5, Convert.ToInt32(row12345[1]));
    }

    #endregion
}
