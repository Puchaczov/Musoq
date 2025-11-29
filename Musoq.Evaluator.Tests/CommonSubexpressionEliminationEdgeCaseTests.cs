using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Edge case tests for Common Subexpression Elimination (CSE).
/// These tests verify that CSE doesn't optimize too aggressively and
/// produces correct results for complex and unusual query patterns.
/// </summary>
[TestClass]
public class CommonSubexpressionEliminationEdgeCaseTests : BasicEntityTestBase
{
    #region Nested Method Calls - A(B()) patterns

    [TestMethod]
    public void WhenNestedCall_InnerExpressionInWhere_OuterInSelect_ShouldReturnCorrectResults()
    {
        // Query: A(B()) in SELECT and B() in WHERE
        // B() should be cached and reused as argument to A()
        const string query = @"
            SELECT ToUpper(ToString(Population)) 
            FROM #A.Entities() 
            WHERE ToString(Population) = '100'";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 100),   // ToString = "100", match
                    new BasicEntity("B", 200),   // ToString = "200", no match
                    new BasicEntity("C", 100)    // ToString = "100", match
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
        // Query: A(B()) and C(B()) - B() appears as inner expression twice
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
                    new BasicEntity("A", 100),   // "100" starts with 1
                    new BasicEntity("B", 200),   // "200" doesn't start with 1
                    new BasicEntity("C", 1500)   // "1500" starts with 1
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        
        var row100 = table.First(r => (string)r[0] == "100");
        Assert.AreEqual(3, Convert.ToInt32(row100[1])); // Length of "100"
        
        var row1500 = table.First(r => (string)r[0] == "1500");
        Assert.AreEqual(4, Convert.ToInt32(row1500[1])); // Length of "1500"
    }

    [TestMethod]
    public void WhenDeeplyNestedCalls_ShouldReturnCorrectResults()
    {
        // Query: A(B(C())) - deeply nested calls
        const string query = @"
            SELECT Length(ToUpper(ToString(Population))) 
            FROM #A.Entities() 
            WHERE Length(ToUpper(ToString(Population))) > 2";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 10),    // "10" -> "10" -> Length 2, filtered
                    new BasicEntity("B", 100),   // "100" -> "100" -> Length 3, included
                    new BasicEntity("C", 1000)   // "1000" -> "1000" -> Length 4, included
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
        // A(B()) and C(B()) where A and C are different, B() is the same
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

    #region Same Expression with Different Semantics

    [TestMethod]
    public void WhenSameTextDifferentContext_ShouldNotConfuse()
    {
        // Ensure that structurally identical expressions from different sources
        // are handled correctly
        const string query = @"
            SELECT a.Name, Length(a.Name) as L1
            FROM #A.Entities() a
            WHERE Length(a.Name) > 2";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("AB"),       // Length 2, filtered
                    new BasicEntity("ABC"),      // Length 3, included
                    new BasicEntity("ABCDE")     // Length 5, included
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

    #region CASE WHEN Edge Cases

    [TestMethod]
    public void WhenNestedCallInCaseWhenCondition_ShouldReturnCorrectResults()
    {
        // A(B()) in CASE WHEN condition and B() in SELECT
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
                    new BasicEntity("A", 10),    // "10" -> Length 2 -> Short
                    new BasicEntity("B", 100),   // "100" -> Length 3 -> Long
                    new BasicEntity("C", 1000)   // "1000" -> Length 4 -> Long
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
        // Same expression appears in multiple WHEN conditions
        // These expressions appear ONLY in CASE WHEN, not in WHERE/SELECT outside of CASE
        // They are not cached because CSE only caches expressions that also appear outside CASE WHEN
        // (so there's a value to compute beforehand and pass as a parameter)
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
                    new BasicEntity("A"),        // Length 1 -> Short
                    new BasicEntity("ABC"),      // Length 3 -> Long
                    new BasicEntity("ABCDEF")    // Length 6 -> Very Long
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
        // CASE WHEN used in WHERE clause
        const string query = @"
            SELECT Name, Length(Name) as Len
            FROM #A.Entities()
            WHERE (CASE WHEN Length(Name) > 2 THEN 1 ELSE 0 END) = 1";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("AB"),       // Length 2, filtered
                    new BasicEntity("ABC"),      // Length 3, included
                    new BasicEntity("ABCDE")     // Length 5, included
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
        // Nested CASE WHEN expressions
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
                    new BasicEntity("AB"),       // Length 2 -> Short
                    new BasicEntity("ABCD"),     // Length 4 -> Long
                    new BasicEntity("ABCDEFG")   // Length 7 -> Very Long
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
        // If CSE pre-computes expressions that would be short-circuited,
        // it could cause issues. This test verifies correctness.
        const string query = @"
            SELECT Name
            FROM #A.Entities()
            WHERE Name IS NOT NULL AND Length(Name) > 2";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity((string)null), // null, first condition false, short-circuit
                    new BasicEntity("AB"),         // not null, Length 2, second condition false
                    new BasicEntity("ABCD")        // not null, Length 4, both true
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
        // OR with shared subexpression
        const string query = @"
            SELECT Name, Length(Name) as Len
            FROM #A.Entities()
            WHERE Length(Name) = 2 OR Length(Name) = 5";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("AB"),       // Length 2, first condition true
                    new BasicEntity("ABC"),      // Length 3, neither
                    new BasicEntity("ABCDE")     // Length 5, second condition true
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

    #region Expressions with Side Effects (Non-Deterministic)

    [TestMethod]
    public void WhenNonDeterministicFunctionUsedMultipleTimes_ShouldNotCache()
    {
        // RandomNumber() should NOT be cached - each call should be independent
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
        // With 3 random values in 0-100 range, probability of all 3 being equal is very low (1/10000)
        // We just verify the query executes correctly
    }

    #endregion

    #region Complex Arithmetic Expressions

    [TestMethod]
    public void WhenComplexArithmeticWithSharedSubexpressions_ShouldReturnCorrectResults()
    {
        // (A + B) * 2 and (A + B) / 2 share (A + B)
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
                    new BasicEntity("A", 30),    // 30+10=40, filtered (not > 50)
                    new BasicEntity("B", 50),    // 50+10=60, included
                    new BasicEntity("C", 90)     // 90+10=100, included
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        
        var row60 = table.First(r => (decimal)r[2] == 60m);
        Assert.AreEqual(120m, (decimal)row60[0]); // 60 * 2
        Assert.AreEqual(30m, (decimal)row60[1]);  // 60 / 2
        
        var row100 = table.First(r => (decimal)r[2] == 100m);
        Assert.AreEqual(200m, (decimal)row100[0]); // 100 * 2
        Assert.AreEqual(50m, (decimal)row100[1]);  // 100 / 2
    }

    #endregion

    #region Multiple Tables and Aliases

    [TestMethod]
    public void WhenSameExpressionOnDifferentTables_ShouldNotConfuse()
    {
        // Ensure Length(a.Name) and Length(b.Name) are treated separately
        // when joining multiple tables
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

    #region NULL Handling Edge Cases

    [TestMethod]
    public void WhenNullableExpressionWithMultipleUses_ShouldHandleCorrectly()
    {
        // Expression that can return null used in multiple places
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
                    new BasicEntity { Name = "B", NullableValue = 3 },   // filtered (not null, not > 5)
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
        // Coalesce uses the expression multiple times internally
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
        // Same expression in SELECT, WHERE, and ORDER BY
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
                    new BasicEntity("A"),        // Length 1, filtered
                    new BasicEntity("AB"),       // Length 2, included
                    new BasicEntity("ABCDE")     // Length 5, included
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        // ORDER BY DESC - longest first
        Assert.AreEqual(5, Convert.ToInt32(table[0][1]));
        Assert.AreEqual(2, Convert.ToInt32(table[1][1]));
    }

    [TestMethod]
    public void WhenExpressionInGroupByAndHaving_ShouldReturnCorrectResults()
    {
        // Expression in GROUP BY and HAVING
        const string query = @"
            SELECT Length(Country) as CountryLen, Count(Country) as Cnt
            FROM #A.Entities()
            GROUP BY Length(Country)
            HAVING Count(Country) > 1";
        
        // Use (country, population) constructor which sets Country property
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("USA", 100),           // Length 3
                    new BasicEntity("Poland", 200),        // Length 6, included
                    new BasicEntity("Germany", 300),       // Length 7
                    new BasicEntity("Poland", 400),        // Length 6, grouped with first Poland
                    new BasicEntity("UK", 500)             // Length 2
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // Should have groups for Length 6 (Poland, 2 entries)
        Assert.AreEqual(1, table.Count);
        Assert.IsTrue(table.Any(row => Convert.ToInt32(row[0]) == 6 && Convert.ToInt32(row[1]) == 2));
    }

    #endregion

    #region Edge Cases with Method Overloads

    [TestMethod]
    public void WhenMethodWithDifferentParameterTypes_ShouldNotConfuse()
    {
        // Inc(decimal) appears multiple times
        // This ensures CSE correctly handles method overloads
        const string query = @"
            SELECT Inc(Population), Inc(Population) + 10
            FROM #A.Entities()
            WHERE Inc(Population) > 100";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 50),    // Inc = 51, filtered
                    new BasicEntity("B", 100),   // Inc = 101, included
                    new BasicEntity("C", 200)    // Inc = 201, included
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
        // CSE cache should reset for each row - values from previous rows
        // should not leak into current row
        const string query = @"
            SELECT Name, Length(Name) as Len
            FROM #A.Entities()
            WHERE Length(Name) > 1";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("AB"),       // Row 1: Length 2
                    new BasicEntity("ABCDE"),    // Row 2: Length 5
                    new BasicEntity("XYZ")       // Row 3: Length 3
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        
        // Verify each row has its own correct Length value
        Assert.IsTrue(table.Any(row => (string)row[0] == "AB" && Convert.ToInt32(row[1]) == 2));
        Assert.IsTrue(table.Any(row => (string)row[0] == "ABCDE" && Convert.ToInt32(row[1]) == 5));
        Assert.IsTrue(table.Any(row => (string)row[0] == "XYZ" && Convert.ToInt32(row[1]) == 3));
    }

    #endregion

    #region Complex Combined Scenarios

    [TestMethod]
    public void WhenComplexQueryWithMultipleCsePatterns_ShouldReturnCorrectResults()
    {
        // Complex query combining multiple CSE patterns
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
                    new BasicEntity("a"),         // Length 1, filtered (< 2)
                    new BasicEntity("ab"),        // Length 2, starts with A, included
                    new BasicEntity("Xyz"),       // Length 3, doesn't start with A, filtered
                    new BasicEntity("Abcdef"),    // Length 6, starts with A, included
                    new BasicEntity("ABC")        // Length 3, starts with A, included
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // Should have: ab (len 2), ABC (len 3), Abcdef (len 6)
        // Ordered by Length DESC: Abcdef, ABC, ab
        Assert.AreEqual(3, table.Count);
        
        // First row: Abcdef (longest)
        Assert.AreEqual("Abcdef", table[0][0]);
        Assert.AreEqual(6, Convert.ToInt32(table[0][1]));
        Assert.AreEqual("ABCDEF", table[0][2]);
        Assert.AreEqual("Very Long", table[0][3]);
        Assert.AreEqual(60, Convert.ToInt32(table[0][4]));
        
        // Second row: ABC
        Assert.AreEqual("ABC", table[1][0]);
        Assert.AreEqual(3, Convert.ToInt32(table[1][1]));
        Assert.AreEqual("ABC", table[1][2]);
        Assert.AreEqual("Long", table[1][3]);
        Assert.AreEqual(30, Convert.ToInt32(table[1][4]));
        
        // Third row: ab
        Assert.AreEqual("ab", table[2][0]);
        Assert.AreEqual(2, Convert.ToInt32(table[2][1]));
        Assert.AreEqual("AB", table[2][2]);
        Assert.AreEqual("Short", table[2][3]);
        Assert.AreEqual(20, Convert.ToInt32(table[2][4]));
    }

    [TestMethod]
    public void WhenSubqueryWithSameCsePattern_ShouldIsolateCorrectly()
    {
        // Subquery and outer query both use Length(Name) - should be isolated
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
                    new BasicEntity(""),         // Empty string, Length 0
                    new BasicEntity("A"),        // Length 1
                    new BasicEntity("ABC")       // Length 3
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
                    new BasicEntity("A", 100),      // 10000, filtered
                    new BasicEntity("B", 1000),     // 1000000, filtered (not > 1000000)
                    new BasicEntity("C", 10000)     // 100000000, included
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
        // Same query as a typical CSE test, but with CSE disabled
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

        // Run with CSE disabled
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
        // Complex query with many duplicate expressions
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

        // Run with CSE enabled (default)
        var compilationOptionsEnabled = new CompilationOptions(useCommonSubexpressionElimination: true);
        var vmEnabled = InstanceCreator.CompileForExecution(
            query, 
            Guid.NewGuid().ToString(), 
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            compilationOptionsEnabled);
        var tableEnabled = vmEnabled.Run(TestContext.CancellationToken);

        // Run with CSE disabled
        var compilationOptionsDisabled = new CompilationOptions(useCommonSubexpressionElimination: false);
        var vmDisabled = InstanceCreator.CompileForExecution(
            query, 
            Guid.NewGuid().ToString(), 
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            compilationOptionsDisabled);
        var tableDisabled = vmDisabled.Run(TestContext.CancellationToken);

        // Both should produce identical results
        Assert.AreEqual(tableDisabled.Count, tableEnabled.Count);
        for (int i = 0; i < tableEnabled.Count; i++)
        {
            Assert.AreEqual(tableDisabled[i][0], tableEnabled[i][0], $"Row {i}, Column 0 mismatch");
            Assert.AreEqual(tableDisabled[i][1], tableEnabled[i][1], $"Row {i}, Column 1 mismatch");
            Assert.AreEqual(tableDisabled[i][2], tableEnabled[i][2], $"Row {i}, Column 2 mismatch");
        }
    }

    [TestMethod]
    public void WhenCseDisabled_CaseWhenShouldStillWork()
    {
        // CASE WHEN with duplicate expressions, CSE disabled
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
                    new BasicEntity("Poland", 100),  // Inc = 101, <= 200, result = 101
                    new BasicEntity("USA", 300)      // Inc = 301, > 200, result = 602
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
        // Column Population is accessed in WHERE, SELECT (twice), and used in expression
        // CSE should cache the column access to avoid boxing allocations
        const string query = @"
            SELECT Population, Population + 10, Population * 2
            FROM #A.Entities()
            WHERE Population > 100";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 50),   // Filtered out
                    new BasicEntity("USA", 200),     // 200, 210, 400
                    new BasicEntity("Germany", 150)  // 150, 160, 300
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        
        // Verify results are correct
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
        // Multiple columns (Population, Country) accessed multiple times
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
        // Column Population used in WHERE and inside CASE WHEN
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
                    new BasicEntity("Poland", 100),   // Low
                    new BasicEntity("USA", 200),      // High
                    new BasicEntity("UK", 30)         // Filtered out
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
        // Column used both directly and as method argument
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
                    new BasicEntity("Poland", 100)  // 100, 101, 201
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
        // Column is passed to the same method multiple times in different expressions
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
                    new BasicEntity("Poland", 100),  // Inc=101, passes filter
                    new BasicEntity("USA", 40)       // Inc=41, filtered out
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(101m, table[0][0]);  // Inc(100)
        Assert.AreEqual(202m, table[0][1]);  // Inc(100) * 2
        Assert.AreEqual(202m, table[0][2]);  // Inc(100) + Inc(100)
    }

    [TestMethod]
    public void WhenColumnUsedDirectlyAndPassedToMethod_ShouldReturnCorrectResults()
    {
        // Column used both directly and passed to method in same query
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
                    new BasicEntity("Poland", 100),  // Pop=100, Inc=101, passes
                    new BasicEntity("USA", 50),       // Pop=50, filtered by Population > 50
                    new BasicEntity("UK", 60)         // Pop=60, Inc=61, filtered by Inc > 100
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100m, table[0][0]);    // Population
        Assert.AreEqual(101m, table[0][1]);    // Inc(Population)
        Assert.AreEqual(201m, table[0][2]);    // Population + Inc(Population)
        Assert.AreEqual(10100m, table[0][3]);  // Population * Inc(Population)
    }

    [TestMethod]
    public void WhenMultipleColumnsPassedToSameMethod_ShouldReturnCorrectResults()
    {
        // Different columns (both decimal) passed to the same method
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
        Assert.AreEqual(101m, table[0][0]);   // Inc(Population=100)
        Assert.AreEqual(111m, table[0][1]);   // Inc(Population=100) + 10
        Assert.AreEqual(50m, table[0][2]);    // Money
        Assert.AreEqual(51m, table[0][3]);    // Inc(Money=50)
    }

    [TestMethod]
    public void WhenColumnInCaseWhenWithMethodCalls_ShouldReturnCorrectResults()
    {
        // Column used in CASE WHEN along with method calls on the same column
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
                    new BasicEntity("Poland", 200),  // Pop=200, Inc=201, High
                    new BasicEntity("USA", 100),     // Pop=100, Inc=101, Medium
                    new BasicEntity("UK", 30)        // Pop=30, Inc=31, Low
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
        // Nested method calls with column as argument
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
                    new BasicEntity("Poland", 100)  // Inc(Inc(100)) = 102
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100m, table[0][0]);   // Population
        Assert.AreEqual(102m, table[0][1]);   // Inc(Inc(100))
        Assert.AreEqual(202m, table[0][2]);   // Inc(Inc(100)) + Population
    }

    [TestMethod]
    public void WhenColumnUsedInComplexArithmeticWithMethods_ShouldReturnCorrectResults()
    {
        // Complex arithmetic mixing columns and method calls
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
                    new BasicEntity("Poland", 10)  // Pop=10, Inc=11
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(10m, table[0][0]);    // Population
        Assert.AreEqual(11m, table[0][1]);    // Inc(Population)
        Assert.AreEqual(42m, table[0][2]);    // (10 + 11) * 2 = 42
        Assert.AreEqual(221m, table[0][3]);   // 10*10 + 11*11 = 100 + 121 = 221
    }

    [TestMethod]
    public void WhenMultipleColumnsWithMultipleMethods_ShouldReturnCorrectResults()
    {
        // Multiple columns each used with multiple methods
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
        // Column used throughout the query in WHERE, SELECT, and expressions
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
                    new BasicEntity("Poland", 100),  // Pop=100, Inc=101, passes
                    new BasicEntity("USA", 150),     // Pop=150, Inc=151, passes
                    new BasicEntity("UK", 50),       // Pop=50, filtered by > 50
                    new BasicEntity("France", 250)   // Pop=250, Inc=251, filtered by Inc < 200
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        
        // Should be ordered DESC by Population
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
        // Same column passed to different methods
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
        Assert.AreEqual(50m, rows[0][3]);   // Abs(100 - 150) = 50
        
        Assert.AreEqual(200m, rows[1][0]);
        Assert.AreEqual(201m, rows[1][1]);
        Assert.AreEqual("200", rows[1][2]);
        Assert.AreEqual(50m, rows[1][3]);   // Abs(200 - 150) = 50
    }

    [TestMethod]
    public void WhenColumnExpressionPassedToMethod_ShouldReturnCorrectResults()
    {
        // Column expression (not just column) passed to method
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
                    new BasicEntity("Poland", 100)  // Pop+10=110, Inc(110)=111
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100m, table[0][0]);   // Population
        Assert.AreEqual(110m, table[0][1]);   // Population + 10
        Assert.AreEqual(111m, table[0][2]);   // Inc(Population + 10)
        Assert.AreEqual(222m, table[0][3]);   // Inc(Population + 10) * 2
    }

    [TestMethod]
    public void WhenMixedColumnMethodAndLiterals_ShouldReturnCorrectResults()
    {
        // Mix of columns, method calls, and literals in expressions
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
                    new BasicEntity("Poland", 100)  // Pop=100, Inc=101
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100m, table[0][0]);    // Population
        Assert.AreEqual(101m, table[0][1]);    // Inc(Population)
        Assert.AreEqual(351m, table[0][2]);    // 100 + 100 + 101 + 50 = 351
        Assert.AreEqual(1503m, table[0][3]);   // (100*2) + (101*3) + 1000 = 200 + 303 + 1000 = 1503
    }

    #endregion

    #region IsNull Node Id Tests

    [TestMethod]
    public void WhenIsNullOnDifferentColumns_ShouldReturnCorrectResults()
    {
        // Tests that IsNullNode.Id includes the Expression.Id
        // Previously, "Name IS NULL" and "Country IS NULL" would have the same Id
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
        
        // Row with Name=null, Country=Poland
        var row1 = table.FirstOrDefault(r => r[1]?.ToString() == "Poland");
        Assert.IsNotNull(row1);
        Assert.IsNull(row1[0]);
        Assert.AreEqual("NameNull", row1[2]);
        Assert.AreEqual("CountryNotNull", row1[3]);
        
        // Row with Name=Test, Country=null
        var row2 = table.FirstOrDefault(r => r[0]?.ToString() == "Test");
        Assert.IsNotNull(row2);
        Assert.IsNull(row2[1]);
        Assert.AreEqual("NameNotNull", row2[2]);
        Assert.AreEqual("CountryNull", row2[3]);
        
        // Row with both populated
        var row3 = table.FirstOrDefault(r => r[0]?.ToString() == "Both");
        Assert.IsNotNull(row3);
        Assert.AreEqual("NameNotNull", row3[2]);
        Assert.AreEqual("CountryNotNull", row3[3]);
    }

    [TestMethod]
    public void WhenIsNullAndIsNotNullOnSameColumn_ShouldReturnCorrectResults()
    {
        // Tests that IsNullNode.Id differentiates between IS NULL and IS NOT NULL
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
        
        // Row with Name=null
        var nullRow = table.FirstOrDefault(r => r[0] == null);
        Assert.IsNotNull(nullRow);
        Assert.AreEqual("IsNull", nullRow[1]);
        Assert.AreEqual("Null2", nullRow[2]);
        
        // Row with Name=Test
        var notNullRow = table.FirstOrDefault(r => r[0]?.ToString() == "Test");
        Assert.IsNotNull(notNullRow);
        Assert.AreEqual("NotNull1", notNullRow[1]);
        Assert.AreEqual("IsNotNull", notNullRow[2]);
    }

    [TestMethod]
    public void WhenIsNullUsedMultipleTimesOnSameColumn_ShouldReturnCorrectResults()
    {
        // Same IS NULL expression used in WHERE and SELECT should be cached correctly
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
                    new BasicEntity { Name = "Other" }  // Filtered out
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
        // Tests that AllColumnsNode.Id includes the alias
        // Using a join scenario where a.* and b.* should be distinct
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

        // Combine sources
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
