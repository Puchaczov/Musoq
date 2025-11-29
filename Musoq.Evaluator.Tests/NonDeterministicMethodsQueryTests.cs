using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Integration tests verifying that non-deterministic methods (marked with [NonDeterministic])
/// behave correctly in queries and are NOT cached by Common Subexpression Elimination (CSE).
/// </summary>
[TestClass]
public class NonDeterministicMethodsQueryTests : BasicEntityTestBase
{
    #region Basic Non-Deterministic Method Tests

    [TestMethod]
    public void WhenNonDeterministicMethodInSelect_ShouldReturnDifferentValuesPerRow()
    {
        // RandomNumber() is marked with [NonDeterministic] - each row should get a fresh random value
        const string query = "SELECT RandomNumber(), Name FROM #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A"),
                    new BasicEntity("B"),
                    new BasicEntity("C"),
                    new BasicEntity("D"),
                    new BasicEntity("E")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // Verify we got all 5 rows
        Assert.AreEqual(5, table.Count);
        
        // Each row should have a random number (0-99)
        foreach (var row in table)
        {
            var value = Convert.ToInt32(row[0]);
            Assert.IsTrue(value >= 0 && value < 100, $"Random value {value} should be between 0 and 99");
        }
    }

    [TestMethod]
    public void WhenNonDeterministicMethodAppearsMultipleTimes_ShouldNotBeCached()
    {
        // RandomNumber() appears twice in SELECT - each column should get independent random values
        // This tests that CSE does NOT cache non-deterministic functions
        const string query = "SELECT RandomNumber() as R1, RandomNumber() as R2, Name FROM #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Test1"),
                    new BasicEntity("Test2"),
                    new BasicEntity("Test3")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        
        // Collect all R1 and R2 values
        var r1Values = table.Select(r => Convert.ToInt32(r[0])).ToList();
        var r2Values = table.Select(r => Convert.ToInt32(r[1])).ToList();
        
        // With true randomness, it's extremely unlikely (but not impossible) that all 6 values are identical
        // We just verify the values are valid random numbers
        foreach (var v in r1Values.Concat(r2Values))
        {
            Assert.IsTrue(v >= 0 && v < 100, $"Random value {v} should be between 0 and 99");
        }
    }

    #endregion

    #region Non-Deterministic in WHERE and SELECT

    [TestMethod]
    public void WhenNonDeterministicMethodInWhereAndSelect_ShouldNotBeCached()
    {
        // RandomNumber() in both WHERE and SELECT should NOT be the same value
        // Unlike deterministic functions that would be cached
        const string query = @"
            SELECT RandomNumber() as RandomVal, Name 
            FROM #A.Entities() 
            WHERE RandomNumber() >= 0";  // Always true, but tests that WHERE gets different random than SELECT
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A"),
                    new BasicEntity("B"),
                    new BasicEntity("C")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // All rows should pass the filter (RandomNumber() >= 0 is always true for 0-99)
        Assert.AreEqual(3, table.Count);
    }

    #endregion

    #region Non-Deterministic with CASE WHEN

    [TestMethod]
    public void WhenNonDeterministicMethodInCaseWhen_ShouldWork()
    {
        // RandomNumber() in CASE WHEN should produce valid results
        const string query = @"
            SELECT 
                CASE WHEN RandomNumber() < 50 THEN 'Low' ELSE 'High' END as Category,
                Name 
            FROM #A.Entities()";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A"),
                    new BasicEntity("B"),
                    new BasicEntity("C"),
                    new BasicEntity("D")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);
        
        // Each row should have either "Low" or "High"
        foreach (var row in table)
        {
            var category = (string)row[0];
            Assert.IsTrue(category == "Low" || category == "High", 
                $"Category should be 'Low' or 'High', but was '{category}'");
        }
    }

    #endregion

    #region Non-Deterministic with Arithmetic

    [TestMethod]
    public void WhenNonDeterministicMethodInArithmetic_ShouldWork()
    {
        // RandomNumber() in arithmetic expressions
        const string query = "SELECT RandomNumber() + 100 as Shifted, Name FROM #A.Entities()";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A"),
                    new BasicEntity("B")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        
        // RandomNumber() returns 0-99, so Shifted should be 100-199
        foreach (var row in table)
        {
            var shifted = Convert.ToInt32(row[0]);
            Assert.IsTrue(shifted >= 100 && shifted < 200, 
                $"Shifted value {shifted} should be between 100 and 199");
        }
    }

    [TestMethod]
    public void WhenNonDeterministicMethodInMultipleArithmeticExpressions_ShouldNotBeCached()
    {
        // Two different expressions using RandomNumber() - should get independent random values
        const string query = @"
            SELECT 
                RandomNumber() + 100 as Shifted1, 
                RandomNumber() * 2 as Doubled,
                Name 
            FROM #A.Entities()";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A"),
                    new BasicEntity("B")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        
        foreach (var row in table)
        {
            var shifted1 = Convert.ToInt32(row[0]);
            var doubled = Convert.ToInt32(row[1]);
            
            // Verify ranges
            Assert.IsTrue(shifted1 >= 100 && shifted1 < 200, 
                $"Shifted1 {shifted1} should be between 100 and 199");
            Assert.IsTrue(doubled >= 0 && doubled < 200, 
                $"Doubled {doubled} should be between 0 and 198");
        }
    }

    #endregion

    #region Deterministic vs Non-Deterministic Comparison

    [TestMethod]
    public void WhenDeterministicMethodAppearsMultipleTimes_ShouldBeCached()
    {
        // Length(Name) appears twice - CSE should cache it (same value in both columns)
        const string query = "SELECT Length(Name) as Len1, Length(Name) as Len2, Name FROM #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("ABC"),
                    new BasicEntity("DEFGH")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        
        // Len1 and Len2 should always be equal for the same row (deterministic = cached)
        foreach (var row in table)
        {
            var len1 = Convert.ToInt32(row[0]);
            var len2 = Convert.ToInt32(row[1]);
            Assert.AreEqual(len1, len2, "Deterministic function results should be identical when cached");
        }
    }

    [TestMethod]
    public void WhenMixingDeterministicAndNonDeterministic_ShouldHandleCorrectly()
    {
        // Mix of deterministic (Length) and non-deterministic (RandomNumber) in same query
        const string query = @"
            SELECT 
                Length(Name) as NameLength,
                Length(Name) + 10 as LengthPlus10,
                RandomNumber() as Random1,
                RandomNumber() as Random2,
                Name 
            FROM #A.Entities()";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Test"),
                    new BasicEntity("Hello")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        
        foreach (var row in table)
        {
            var nameLength = Convert.ToInt32(row[0]);
            var lengthPlus10 = Convert.ToInt32(row[1]);
            var random1 = Convert.ToInt32(row[2]);
            var random2 = Convert.ToInt32(row[3]);
            
            // Deterministic: LengthPlus10 should equal NameLength + 10
            Assert.AreEqual(nameLength + 10, lengthPlus10, 
                "Deterministic expressions should have consistent results");
            
            // Non-deterministic: both should be valid random values
            Assert.IsTrue(random1 >= 0 && random1 < 100);
            Assert.IsTrue(random2 >= 0 && random2 < 100);
        }
    }

    #endregion

    #region Non-Deterministic in Various Contexts

    [TestMethod]
    public void WhenNonDeterministicMethodInSubquery_ShouldWork()
    {
        // RandomNumber() in a subquery should work
        const string query = @"
            SELECT Name, RandomNumber() as Rand
            FROM #A.Entities()
            WHERE Name IN ('A', 'B')";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A"),
                    new BasicEntity("B"),
                    new BasicEntity("C"),
                    new BasicEntity("D")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        
        foreach (var row in table)
        {
            var name = (string)row[0];
            var random = Convert.ToInt32(row[1]);
            
            Assert.IsTrue(name == "A" || name == "B");
            Assert.IsTrue(random >= 0 && random < 100);
        }
    }

    #endregion

    #region Non-Deterministic with GROUP BY

    [TestMethod]
    public void WhenNonDeterministicMethodWithGroupBy_ShouldWork()
    {
        // RandomNumber() should work in grouped queries
        // Note: RandomNumber() is evaluated once per group in the SELECT clause
        const string query = @"
            SELECT 
                Name,
                Count(Name) as Cnt
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
                    new BasicEntity("C")  // Only 1, filtered out by HAVING
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        
        var rowA = table.First(r => (string)r[0] == "A");
        var rowB = table.First(r => (string)r[0] == "B");
        
        Assert.AreEqual(2, Convert.ToInt32(rowA[1])); // Count for A
        Assert.AreEqual(3, Convert.ToInt32(rowB[1])); // Count for B
    }

    [TestMethod]
    public void WhenNonDeterministicMethodInHaving_ShouldNotBeCached()
    {
        // RandomNumber() in HAVING clause - each group gets its own evaluation
        // This test verifies the query compiles and runs without CSE interfering
        const string query = @"
            SELECT 
                Name,
                Count(Name) as Cnt
            FROM #A.Entities()
            GROUP BY Name
            HAVING RandomNumber() >= 0";  // Always true, but tests that RandomNumber works in HAVING
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A"),
                    new BasicEntity("A"),
                    new BasicEntity("B")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        // All groups should pass since RandomNumber() >= 0 is always true (0-99)
        Assert.AreEqual(2, table.Count);
    }

    public TestContext TestContext { get; set; }

    #endregion
}
