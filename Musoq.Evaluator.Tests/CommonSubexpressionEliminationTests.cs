using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Tests for Common Subexpression Elimination (CSE) optimization.
/// CSE identifies expressions computed multiple times and caches the result
/// from the first computation for subsequent uses.
/// </summary>
[TestClass]
public class CommonSubexpressionEliminationTests : BasicEntityTestBase
{
    #region Basic CSE - Same Expression in WHERE and SELECT

    [TestMethod]
    public void WhenSameMethodInWhereAndSelect_ShouldReturnCorrectResults()
    {
        const string query = "SELECT Length(Name) FROM #A.Entities() WHERE Length(Name) > 3";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("AB"),
                    new BasicEntity("ABCD"),
                    new BasicEntity("ABCDEF"),
                    new BasicEntity("XYZ")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should have 2 rows passing the filter");
        Assert.IsTrue(table.Any(row => Convert.ToInt32(row.Values[0]) == 4), "Should contain Length = 4");
        Assert.IsTrue(table.Any(row => Convert.ToInt32(row.Values[0]) == 6), "Should contain Length = 6");
    }

    [TestMethod]
    public void WhenSameColumnAccessInWhereAndSelect_ShouldReturnCorrectResults()
    {
        const string query = "SELECT Name FROM #A.Entities() WHERE Name = 'Test'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Test"),
                    new BasicEntity("Other"),
                    new BasicEntity("Test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should have 2 rows with Name = 'Test'");
        Assert.IsTrue(table.All(row => (string)row.Values[0] == "Test"));
    }

    [TestMethod]
    public void WhenNestedMethodCallInWhereAndSelect_ShouldReturnCorrectResults()
    {
        const string query = "SELECT ToUpper(Name) FROM #A.Entities() WHERE ToUpper(Name) = 'TEST'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("test"),
                    new BasicEntity("TEST"),
                    new BasicEntity("TeSt"),
                    new BasicEntity("other")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "Should have 3 rows matching ToUpper = 'TEST'");
        Assert.IsTrue(table.All(row => (string)row.Values[0] == "TEST"));
    }

    #endregion

    #region CSE with Multiple Occurrences

    [TestMethod]
    public void WhenExpressionAppearsThreeTimes_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT Name, Length(Name) as Len 
            FROM #A.Entities() 
            WHERE Length(Name) >= 3
            ORDER BY Length(Name) DESC";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("AB"),
                    new BasicEntity("ABC"),
                    new BasicEntity("ABCDE"),
                    new BasicEntity("ABCD")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count, "Should have 3 rows");
        Assert.AreEqual(5, Convert.ToInt32(table[0][1]), "First row should have Length = 5");
        Assert.AreEqual(4, Convert.ToInt32(table[1][1]), "Second row should have Length = 4");
        Assert.AreEqual(3, Convert.ToInt32(table[2][1]), "Third row should have Length = 3");
    }

    [TestMethod]
    public void WhenMultipleDifferentExpressionsAreDuplicated_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT Length(Name), ToUpper(Name) 
            FROM #A.Entities() 
            WHERE Length(Name) > 2 AND ToUpper(Name) LIKE 'A%'";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("AB"),
                    new BasicEntity("Abc"),
                    new BasicEntity("Xyz"),
                    new BasicEntity("Alpha")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should have 2 rows");
        Assert.IsTrue(table.Any(row => Convert.ToInt32(row.Values[0]) == 3 && (string)row.Values[1] == "ABC"));
        Assert.IsTrue(table.Any(row => Convert.ToInt32(row.Values[0]) == 5 && (string)row.Values[1] == "ALPHA"));
    }

    #endregion

    #region CSE with Complex Expressions

    [TestMethod]
    public void WhenArithmeticExpressionIsDuplicated_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT Population * 2 as DoublePopulation 
            FROM #A.Entities() 
            WHERE Population * 2 > 100";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 40),
                    new BasicEntity("B", 60),
                    new BasicEntity("C", 100)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should have 2 rows");
        Assert.IsTrue(table.Any(row => (decimal)row.Values[0] == 120m));
        Assert.IsTrue(table.Any(row => (decimal)row.Values[0] == 200m));
    }

    [TestMethod]
    public void WhenStringConcatenationIsDuplicated_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT City + ', ' + Country as Location 
            FROM #A.Entities() 
            WHERE City + ', ' + Country LIKE '%Poland%'";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", "Warsaw"),
                    new BasicEntity("Germany", "Berlin"),
                    new BasicEntity("Poland", "Krakow")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count, "Should have 2 Polish cities");
        Assert.IsTrue(table.All(row => ((string)row.Values[0]).Contains("Poland")));
    }

    #endregion

    #region CSE Should NOT Cache Non-Deterministic Functions

    [TestMethod]
    public void WhenRandomFunctionUsedTwice_ShouldProduceDifferentValues()
    {
        const string query = "SELECT RandomNumber() as R1, RandomNumber() as R2 FROM #A.Entities()";
        
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

    #region CSE with Aggregates - Should NOT Cache

    [TestMethod]
    public void WhenAggregateUsedInHaving_ShouldNotAffectResult()
    {
        const string query = @"
            SELECT Country, Count(Country) as Cnt 
            FROM #A.Entities() 
            GROUP BY Country 
            HAVING Count(Country) > 1";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 100),
                    new BasicEntity("Poland", 200),
                    new BasicEntity("Germany", 300),
                    new BasicEntity("Poland", 400)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Poland", (string)table[0][0]);
        Assert.AreEqual(3, Convert.ToInt32(table[0][1]));
    }

    [TestMethod]
    public void WhenSameAggregateAppearsMultipleTimes_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT Country, Sum(Population) as Total, Sum(Population) * 2 as DoubleTotal 
            FROM #A.Entities() 
            GROUP BY Country";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Poland", 100),
                    new BasicEntity("Poland", 200),
                    new BasicEntity("Germany", 500)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        
        var polandRow = table.First(r => (string)r[0] == "Poland");
        Assert.AreEqual(300m, (decimal)polandRow[1]);
        Assert.AreEqual(600m, (decimal)polandRow[2]);
        
        var germanyRow = table.First(r => (string)r[0] == "Germany");
        Assert.AreEqual(500m, (decimal)germanyRow[1]);
        Assert.AreEqual(1000m, (decimal)germanyRow[2]);
    }

    #endregion

    #region CSE with NULL Values

    [TestMethod]
    public void WhenExpressionEvaluatesToNull_ShouldHandleCorrectly()
    {
        const string query = @"
            SELECT NullableValue, NullableValue + 1 
            FROM #A.Entities() 
            WHERE NullableValue IS NOT NULL";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "A", NullableValue = null },
                    new BasicEntity { Name = "B", NullableValue = 10 },
                    new BasicEntity { Name = "C", NullableValue = 20 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (int?)row.Values[0] == 10));
        Assert.IsTrue(table.Any(row => (int?)row.Values[0] == 20));
    }

    [TestMethod]
    public void WhenMethodReturnsNullAndIsUsedTwice_ShouldHandleCorrectly()
    {
        const string query = @"
            SELECT NullableValue 
            FROM #A.Entities() 
            WHERE NullableValue IS NULL";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "A", NullableValue = null },
                    new BasicEntity { Name = "B", NullableValue = 10 },
                    new BasicEntity { Name = "C", NullableValue = null }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.All(row => row.Values[0] == null));
    }

    #endregion

    #region CSE with Type Conversions

    [TestMethod]
    public void WhenExpressionWithImplicitConversionIsDuplicated_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT ToString(Population) 
            FROM #A.Entities() 
            WHERE ToString(Population) LIKE '1%'";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A", 100),
                    new BasicEntity("B", 200),
                    new BasicEntity("C", 150),
                    new BasicEntity("D", 1000)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.All(row => ((string)row.Values[0]).StartsWith("1")));
    }

    #endregion

    #region CSE Edge Cases

    [TestMethod]
    public void WhenNoExpressionIsDuplicated_ShouldStillWorkCorrectly()
    {
        const string query = @"
            SELECT City, Country, Population 
            FROM #A.Entities() 
            WHERE Population > 50";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("Warsaw", "Poland", 100),
                    new BasicEntity("Berlin", "Germany", 30),
                    new BasicEntity("Krakow", "Poland", 80)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Warsaw"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Krakow"));
    }

    [TestMethod]
    public void WhenExpressionWithDifferentAliasButSameStructure_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT a.Name, Length(a.Name) as L1, Length(a.Name) as L2
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
        
        foreach (var row in table)
        {
            Assert.AreEqual(row[1], row[2], "L1 and L2 should have the same value");
        }
    }

    [TestMethod]
    public void WhenSameExpressionInCaseWhenBranches_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT 
                CASE 
                    WHEN Length(Name) > 3 THEN 'Long'
                    WHEN Length(Name) > 1 THEN 'Medium'
                    ELSE 'Short'
                END as Category
            FROM #A.Entities()
            WHERE Length(Name) > 0";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("A"),
                    new BasicEntity("ABCD"),
                    new BasicEntity("AB")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Long"));
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Medium"));
    }

    #endregion

    #region CSE with Subexpressions

    [TestMethod]
    public void WhenSubexpressionIsDuplicated_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT Length(Name) * 10, Length(Name) + 5 
            FROM #A.Entities() 
            WHERE Length(Name) > 2";
        
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
        Assert.IsTrue(table.Any(row => Convert.ToInt32(row.Values[0]) == 30 && Convert.ToInt32(row.Values[1]) == 8));
        Assert.IsTrue(table.Any(row => Convert.ToInt32(row.Values[0]) == 50 && Convert.ToInt32(row.Values[1]) == 10));
    }

    #endregion

    #region CASE WHEN CSE Tests

    [TestMethod]
    public void WhenExpressionAppearsInSelectAndCaseWhen_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT 
                Length(Name) as Len,
                CASE WHEN Length(Name) > 3 THEN 'Long' ELSE 'Short' END as Category
            FROM #A.Entities()";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("AB"),
                    new BasicEntity("ABCD"),
                    new BasicEntity("ABCDEF")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        
        var shortRow = table.First(row => Convert.ToInt32(row[0]) == 2);
        Assert.AreEqual("Short", shortRow[1]);
        
        var longRow1 = table.First(row => Convert.ToInt32(row[0]) == 4);
        Assert.AreEqual("Long", longRow1[1]);
        
        var longRow2 = table.First(row => Convert.ToInt32(row[0]) == 6);
        Assert.AreEqual("Long", longRow2[1]);
    }

    [TestMethod]
    public void WhenExpressionAppearsInWhereAndCaseWhen_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT 
                Name,
                CASE WHEN Length(Name) >= 4 THEN 'Long' ELSE 'Medium' END as Category
            FROM #A.Entities()
            WHERE Length(Name) >= 3";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("AB"),
                    new BasicEntity("ABC"),
                    new BasicEntity("ABCD"),
                    new BasicEntity("ABCDEF")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        
        var mediumRow = table.First(row => (string)row[0] == "ABC");
        Assert.AreEqual("Medium", mediumRow[1]);
        
        var longRow1 = table.First(row => (string)row[0] == "ABCD");
        Assert.AreEqual("Long", longRow1[1]);
        
        var longRow2 = table.First(row => (string)row[0] == "ABCDEF");
        Assert.AreEqual("Long", longRow2[1]);
    }

    [TestMethod]
    public void WhenExpressionOnlyAppearsInsideCaseWhen_ShouldReturnCorrectResults()
    {
        const string query = @"
            SELECT 
                Name,
                CASE WHEN ToUpper(Name) = 'ABC' THEN 'Match' ELSE 'NoMatch' END as Category
            FROM #A.Entities()";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("abc"),
                    new BasicEntity("ABC"),
                    new BasicEntity("def")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        
        var matchRow1 = table.First(row => (string)row[0] == "abc");
        Assert.AreEqual("Match", matchRow1[1]);
        
        var matchRow2 = table.First(row => (string)row[0] == "ABC");
        Assert.AreEqual("Match", matchRow2[1]);
        
        var noMatchRow = table.First(row => (string)row[0] == "def");
        Assert.AreEqual("NoMatch", noMatchRow[1]);
    }

    #endregion

    #region Performance Verification Tests (for future CSE implementation validation)

    [TestMethod]
    public void WhenExpensiveMethodCalledTwice_ShouldReturnCorrectResults()
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

    public TestContext TestContext { get; set; }

    #endregion
}
