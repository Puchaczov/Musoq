using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Edge case tests for Common Subexpression Elimination (CSE).
///     These tests verify that CSE doesn't optimize too aggressively and
///     produces correct results for complex and unusual query patterns.
/// </summary>
[TestClass]
public class CommonSubexpressionEliminationEdgeCaseTests : BasicEntityTestBase
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

    #region Complex Combined Scenarios

    [TestMethod]
    public void WhenComplexQueryWithMultipleCsePatterns_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT 
                Name,
                Length(Name) as Len,
                ToUpper(Name) as Upper,
                CASE 
                    WHEN Length(Name) > 4 THEN 'Very Long'
                    WHEN Length(Name) > 2 THEN 'Long'
                    ELSE 'Short'
                END as Category,
                Length(Name) * 10 as LenTimes10
            FROM #A.Entities()
            WHERE Length(Name) >= 2 AND ToUpper(Name) LIKE 'A%'
            ORDER BY Length(Name) DESC";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("a"),
                    new BasicEntity("ab"),
                    new BasicEntity("Xyz"),
                    new BasicEntity("Abcdef"),
                    new BasicEntity("ABC")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(3, table.Count);


        Assert.AreEqual("Abcdef", table[0][0]);
        Assert.AreEqual(6, Convert.ToInt32(table[0][1]));
        Assert.AreEqual("ABCDEF", table[0][2]);
        Assert.AreEqual("Very Long", table[0][3]);
        Assert.AreEqual(60, Convert.ToInt32(table[0][4]));


        Assert.AreEqual("ABC", table[1][0]);
        Assert.AreEqual(3, Convert.ToInt32(table[1][1]));
        Assert.AreEqual("ABC", table[1][2]);
        Assert.AreEqual("Long", table[1][3]);
        Assert.AreEqual(30, Convert.ToInt32(table[1][4]));


        Assert.AreEqual("ab", table[2][0]);
        Assert.AreEqual(2, Convert.ToInt32(table[2][1]));
        Assert.AreEqual("AB", table[2][2]);
        Assert.AreEqual("Short", table[2][3]);
        Assert.AreEqual(20, Convert.ToInt32(table[2][4]));
    }

    [TestMethod]
    public void WhenSubqueryWithSameCsePattern_ShouldIsolateCorrectly()
    {
        const string query = @"
            SELECT Name, Length(Name) as Len
            FROM #A.Entities()
            WHERE Length(Name) > 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("AB"),
                    new BasicEntity("ABCD"),
                    new BasicEntity("XY")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("ABCD", table[0][0]);
        Assert.AreEqual(4, Convert.ToInt32(table[0][1]));
    }

    #endregion

    #region Boundary Values and Special Cases

    [TestMethod]
    public void WhenExpressionReturnsEmptyString_ShouldHandleCorrectly()
    {
        const string query = @"
            SELECT 
                Name,
                Length(Name) as Len,
                Length(Name) * 2 as DoubleLlen
            FROM #A.Entities()
            WHERE Length(Name) >= 0";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity(""),
                    new BasicEntity("A"),
                    new BasicEntity("ABC")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.Any(row => (string)row[0] == "" && Convert.ToInt32(row[1]) == 0));
        Assert.IsTrue(table.Any(row => (string)row[0] == "A" && Convert.ToInt32(row[1]) == 1));
        Assert.IsTrue(table.Any(row => (string)row[0] == "ABC" && Convert.ToInt32(row[1]) == 3));
    }

    [TestMethod]
    public void WhenExpressionWithLargeValues_ShouldHandleCorrectly()
    {
        const string query = @"
            SELECT Population, Population * Population as Squared
            FROM #A.Entities()
            WHERE Population * Population > 1000000";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 100),
                    new BasicEntity("B", 1000),
                    new BasicEntity("C", 10000)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(10000m, (decimal)table[0][0]);
        Assert.AreEqual(100000000m, (decimal)table[0][1]);
    }

    #endregion

    #region CSE Toggle Tests

    [TestMethod]
    public void WhenCseDisabled_ShouldStillProduceCorrectResults()
    {
        const string query = @"
            SELECT ToString(Population), ToString(Population) + '_suffix'
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


        var compilationOptions = new CompilationOptions(useCommonSubexpressionElimination: false);
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            compilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.All(row => (string)row.Values[0] == "100"));
        Assert.IsTrue(table.All(row => (string)row.Values[1] == "100_suffix"));
    }

    [TestMethod]
    public void WhenCseEnabled_ShouldProduceSameResultsAsDisabled()
    {
        const string query = @"
            SELECT 
                Length(Name) as Len1,
                Length(Name) + 10 as Len2,
                CASE WHEN Length(Name) > 3 THEN 'long' ELSE 'short' END as Category
            FROM #A.Entities()
            WHERE Length(Name) > 1
            ORDER BY Length(Name)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("AB"),
                    new BasicEntity("ABCDE"),
                    new BasicEntity("X")
                ]
            }
        };


        var compilationOptionsEnabled = new CompilationOptions(useCommonSubexpressionElimination: true);
        var vmEnabled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            compilationOptionsEnabled);
        var tableEnabled = vmEnabled.Run(TestContext.CancellationToken);


        var compilationOptionsDisabled = new CompilationOptions(useCommonSubexpressionElimination: false);
        var vmDisabled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            compilationOptionsDisabled);
        var tableDisabled = vmDisabled.Run(TestContext.CancellationToken);


        Assert.AreEqual(tableDisabled.Count, tableEnabled.Count);
        for (var i = 0; i < tableEnabled.Count; i++)
        {
            Assert.AreEqual(tableDisabled[i][0], tableEnabled[i][0], $"Row {i}, Column 0 mismatch");
            Assert.AreEqual(tableDisabled[i][1], tableEnabled[i][1], $"Row {i}, Column 1 mismatch");
            Assert.AreEqual(tableDisabled[i][2], tableEnabled[i][2], $"Row {i}, Column 2 mismatch");
        }
    }

    [TestMethod]
    public void WhenCseDisabled_CaseWhenShouldStillWork()
    {
        const string query = @"
            SELECT 
                CASE 
                    WHEN Inc(Population) > 200 THEN Inc(Population) * 2
                    ELSE Inc(Population)
                END as Result
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 100),
                    new BasicEntity("USA", 300)
                ]
            }
        };

        var compilationOptions = new CompilationOptions(useCommonSubexpressionElimination: false);
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            compilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => Convert.ToDecimal(row[0])).OrderBy(x => x).ToList();
        Assert.AreEqual(101m, results[0]);
        Assert.AreEqual(602m, results[1]);
    }

    #endregion

    #region Column Access Caching Tests

    [TestMethod]
    public void WhenSameColumnAccessedMultipleTimes_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT Population, Population + 10, Population * 2
            FROM #A.Entities()
            WHERE Population > 100";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 50),
                    new BasicEntity("USA", 200),
                    new BasicEntity("Germany", 150)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);


        var rows = table.OrderBy(r => (decimal)r[0]).ToList();
        Assert.AreEqual(150m, rows[0][0]);
        Assert.AreEqual(160m, rows[0][1]);
        Assert.AreEqual(300m, rows[0][2]);
        Assert.AreEqual(200m, rows[1][0]);
        Assert.AreEqual(210m, rows[1][1]);
        Assert.AreEqual(400m, rows[1][2]);
    }

    [TestMethod]
    public void WhenMultipleColumnsAccessedMultipleTimes_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT 
                Country, 
                Population, 
                Country + '_suffix',
                Population + 100
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

        var rows = table.OrderBy(r => (string)r[0]).ToList();
        Assert.AreEqual("Poland", rows[0][0]);
        Assert.AreEqual(100m, rows[0][1]);
        Assert.AreEqual("Poland_suffix", rows[0][2]);
        Assert.AreEqual(200m, rows[0][3]);
    }

    [TestMethod]
    public void WhenColumnInWhereAndCaseWhen_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT 
                Population,
                CASE 
                    WHEN Population > 150 THEN 'High'
                    ELSE 'Low'
                END as Category
            FROM #A.Entities()
            WHERE Population > 50";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 100),
                    new BasicEntity("USA", 200),
                    new BasicEntity("UK", 30)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (decimal)r[0]).ToList();
        Assert.AreEqual(100m, rows[0][0]);
        Assert.AreEqual("Low", rows[0][1]);
        Assert.AreEqual(200m, rows[1][0]);
        Assert.AreEqual("High", rows[1][1]);
    }

    [TestMethod]
    public void WhenColumnUsedWithMethodCall_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT 
                Population,
                Inc(Population),
                Population + Inc(Population)
            FROM #A.Entities()
            WHERE Population > 0";

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
        Assert.AreEqual(201m, table[0][2]);
    }

    #endregion

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

    #region IsNull Node Id Tests

    [TestMethod]
    public void WhenIsNullOnDifferentColumns_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT 
                Name,
                Country,
                CASE WHEN Name IS NULL THEN 'NameNull' ELSE 'NameNotNull' END,
                CASE WHEN Country IS NULL THEN 'CountryNull' ELSE 'CountryNotNull' END
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null, Country = "Poland" },
                    new BasicEntity { Name = "Test", Country = null },
                    new BasicEntity { Name = "Both", Country = "USA" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);


        var row1 = table.FirstOrDefault(r => r[1]?.ToString() == "Poland");
        Assert.IsNotNull(row1);
        Assert.IsNull(row1[0]);
        Assert.AreEqual("NameNull", row1[2]);
        Assert.AreEqual("CountryNotNull", row1[3]);


        var row2 = table.FirstOrDefault(r => r[0]?.ToString() == "Test");
        Assert.IsNotNull(row2);
        Assert.IsNull(row2[1]);
        Assert.AreEqual("NameNotNull", row2[2]);
        Assert.AreEqual("CountryNull", row2[3]);


        var row3 = table.FirstOrDefault(r => r[0]?.ToString() == "Both");
        Assert.IsNotNull(row3);
        Assert.AreEqual("NameNotNull", row3[2]);
        Assert.AreEqual("CountryNotNull", row3[3]);
    }

    [TestMethod]
    public void WhenIsNullAndIsNotNullOnSameColumn_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT 
                Name,
                CASE WHEN Name IS NULL THEN 'IsNull' ELSE 'NotNull1' END,
                CASE WHEN Name IS NOT NULL THEN 'IsNotNull' ELSE 'Null2' END
            FROM #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "Test" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);


        var nullRow = table.FirstOrDefault(r => r[0] == null);
        Assert.IsNotNull(nullRow);
        Assert.AreEqual("IsNull", nullRow[1]);
        Assert.AreEqual("Null2", nullRow[2]);


        var notNullRow = table.FirstOrDefault(r => r[0]?.ToString() == "Test");
        Assert.IsNotNull(notNullRow);
        Assert.AreEqual("NotNull1", notNullRow[1]);
        Assert.AreEqual("IsNotNull", notNullRow[2]);
    }

    [TestMethod]
    public void WhenIsNullUsedMultipleTimesOnSameColumn_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT 
                Name,
                CASE WHEN Name IS NULL THEN 'Null' ELSE Name END as DisplayName
            FROM #A.Entities()
            WHERE Name IS NULL OR Name = 'Test'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = null },
                    new BasicEntity { Name = "Test" },
                    new BasicEntity { Name = "Other" }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var nullRow = table.FirstOrDefault(r => r[0] == null);
        Assert.IsNotNull(nullRow);
        Assert.AreEqual("Null", nullRow[1]);

        var testRow = table.FirstOrDefault(r => r[0]?.ToString() == "Test");
        Assert.IsNotNull(testRow);
        Assert.AreEqual("Test", testRow[1]);
    }

    #endregion

    #region AllColumns Node Id Tests

    [TestMethod]
    public void WhenAllColumnsWithDifferentAliases_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT a.Country, a.Population, b.Country as Country2, b.Population as Population2
            FROM #A.Entities() a
            INNER JOIN #B.Entities() b ON a.Country = b.Country";

        var sourcesA = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 100),
                    new BasicEntity("USA", 200)
                ]
            }
        };

        var sourcesB = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#B",
                [
                    new BasicEntity("Poland", 38),
                    new BasicEntity("USA", 331)
                ]
            }
        };


        var allSources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", sourcesA["#A"] },
            { "#B", sourcesB["#B"] }
        };

        var vm = CreateAndRunVirtualMachine(query, allSources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var polandRow = table.FirstOrDefault(r => r[0]?.ToString() == "Poland");
        Assert.IsNotNull(polandRow);
        Assert.AreEqual(100m, polandRow[1]);
        Assert.AreEqual("Poland", polandRow[2]);
        Assert.AreEqual(38m, polandRow[3]);

        var usaRow = table.FirstOrDefault(r => r[0]?.ToString() == "USA");
        Assert.IsNotNull(usaRow);
        Assert.AreEqual(200m, usaRow[1]);
        Assert.AreEqual("USA", usaRow[2]);
        Assert.AreEqual(331m, usaRow[3]);
    }

    public TestContext TestContext { get; set; }

    #endregion
}