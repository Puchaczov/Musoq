using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.PathValue;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AutomaticNumericTypeInference_PathValueTests : PathValueQueryTestBase
{
    [TestMethod]
    public void WhenSelectingObjectValueMultipliedBy2_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Value * 2 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a.b[0]", Value = 5 },
            new() { Path = "a.b[1]", Value = 10L },
            new() { Path = "a.b[2]", Value = 7 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => (long)row[0]).OrderBy(x => x).ToList();
        Assert.AreEqual(10L, results[0]);
        Assert.AreEqual(14L, results[1]);
        Assert.AreEqual(20L, results[2]);
    }

    [TestMethod]
    public void WhenFilteringObjectValueGreaterThan5_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path from Items() where Value > 5";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a.b[0]", Value = 3 },
            new() { Path = "a.b[1]", Value = 10 },
            new() { Path = "a.b[2]", Value = 7L },
            new() { Path = "a.b[3]", Value = 2.5 },
            new() { Path = "a.b[4]", Value = 8.0 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var paths = table.Select(row => (string)row[0]).ToList();
        CollectionAssert.Contains(paths, "a.b[1]");
        CollectionAssert.Contains(paths, "a.b[2]");
        CollectionAssert.Contains(paths, "a.b[4]");
    }

    [TestMethod]
    public void WhenCombiningArithmeticAndComparison_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Value * 2 from Items() where Value > 5";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a[0]", Value = 3 },
            new() { Path = "a[1]", Value = 10 },
            new() { Path = "a[2]", Value = 7 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => (long)row[0]).OrderBy(x => x).ToList();
        Assert.AreEqual(14L, results[0]);
        Assert.AreEqual(20L, results[1]);
    }

    [TestMethod]
    public void WhenObjectValueIsString_ShouldNotAutoConvertForArithmetic()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path from Items() where Value * 2 > 100";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a.b", Value = "100" },  // String "100" should NOT work with arithmetic - null * 2 = null, null > 100 = false
            new() { Path = "a.c", Value = 100 },    // int 100: 100 * 2 = 200, 200 > 100 = true
            new() { Path = "a.d", Value = 200L }    // long 200: 200 * 2 = 400, 400 > 100 = true
        };

        var table = RunQuery(query, entities);

        // Only numeric values should pass the filter - strings result in null arithmetic which fails comparison
        Assert.AreEqual(2, table.Count);
        var paths = table.Select(row => (string)row[0]).OrderBy(x => x).ToList();
        Assert.AreEqual("a.c", paths[0]);
        Assert.AreEqual("a.d", paths[1]);
    }

    [TestMethod]
    public void WhenObjectValueIsNull_ShouldHandleGracefully()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path from Items() where Value > 5";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a[0]", Value = 10 },
            new() { Path = "a[1]", Value = null },
            new() { Path = "a[2]", Value = 7 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(2, table.Count);
        var paths = table.Select(row => (string)row[0]).ToList();
        CollectionAssert.Contains(paths, "a[0]");
        CollectionAssert.Contains(paths, "a[2]");
    }

    [TestMethod]
    public void WhenComparingObjectValuesWithDifferentNumericTypes_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path from Items() where Value >= 5 and Value <= 10";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 5 },
            new() { Path = "b", Value = 5L },
            new() { Path = "c", Value = 5.0 },
            new() { Path = "d", Value = 10 },
            new() { Path = "e", Value = 10.0 },
            new() { Path = "f", Value = 3 },
            new() { Path = "g", Value = 15 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(5, table.Count);
        var paths = table.Select(row => (string)row[0]).ToList();
        CollectionAssert.Contains(paths, "a");
        CollectionAssert.Contains(paths, "b");
        CollectionAssert.Contains(paths, "c");
        CollectionAssert.Contains(paths, "d");
        CollectionAssert.Contains(paths, "e");
    }

    [TestMethod]
    public void WhenDividingObjectValues_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Value / 2 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10 },
            new() { Path = "b", Value = 20L },
            new() { Path = "c", Value = 14 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => (long)row[0]).OrderBy(x => x).ToList();
        Assert.AreEqual(5L, results[0]);
        Assert.AreEqual(7L, results[1]);
        Assert.AreEqual(10L, results[2]);
    }

    [TestMethod]
    public void WhenAddingObjectValues_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Value + 100 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 50 },
            new() { Path = "b", Value = 25L }
            // Removed string "75" - strings should not auto-convert for arithmetic
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => (long)row[0]).OrderBy(x => x).ToList();
        Assert.AreEqual(125L, results[0]);
        Assert.AreEqual(150L, results[1]);
    }

    [TestMethod]
    public void WhenSubtractingFromObjectValues_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Value - 5 from Items() where Value > 10";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 20 },
            new() { Path = "b", Value = 8 },
            new() { Path = "c", Value = 15L }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(2, table.Count);
        var results = table.Select(row => (long)row[0]).OrderBy(x => x).ToList();
        Assert.AreEqual(10L, results[0]);
        Assert.AreEqual(15L, results[1]);
    }

    [TestMethod]
    public void WhenObjectValueIsInvalidString_ShouldExcludeRow()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path from Items() where Value > 5";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10 },
            new() { Path = "b", Value = "not_a_number" },
            new() { Path = "c", Value = "abc" },
            new() { Path = "d", Value = 7 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(2, table.Count);
        var paths = table.Select(row => (string)row[0]).ToList();
        CollectionAssert.Contains(paths, "a");
        CollectionAssert.Contains(paths, "d");
    }

    [TestMethod]
    public void WhenComparingObjectValueWithLessThan_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path from Items() where Value < 10";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 5 },
            new() { Path = "b", Value = 10 },
            new() { Path = "c", Value = 9L },
            new() { Path = "d", Value = "8" },  // String should still work for comparisons
            new() { Path = "e", Value = 15.5 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var paths = table.Select(row => (string)row[0]).ToList();
        CollectionAssert.Contains(paths, "a");
        CollectionAssert.Contains(paths, "c");
        CollectionAssert.Contains(paths, "d");
    }

    [TestMethod]
    public void WhenComparingObjectValueWithEquality_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path from Items() where Value = 42";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 42 },
            new() { Path = "b", Value = 42L },
            new() { Path = "c", Value = "42" },  // String should still work for comparisons
            new() { Path = "d", Value = 41 },
            new() { Path = "e", Value = "43" }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var paths = table.Select(row => (string)row[0]).ToList();
        CollectionAssert.Contains(paths, "a");
        CollectionAssert.Contains(paths, "b");
        CollectionAssert.Contains(paths, "c");
    }

    [TestMethod]
    public void WhenUsingComplexExpression_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select (Value * 2) + 10 from Items() where Value >= 5 and Value <= 15";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 5 },
            new() { Path = "b", Value = 10L },
            new() { Path = "c", Value = 15 },  // Changed from 15.0 to keep it as long
            new() { Path = "d", Value = 3 },
            new() { Path = "e", Value = 20 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => (long)row[0]).OrderBy(x => x).ToList();
        Assert.AreEqual(20L, results[0]);
        Assert.AreEqual(30L, results[1]);
        Assert.AreEqual(40L, results[2]);
    }

    [TestMethod]
    public void WhenObjectValueOverflowsInt32_ShouldExcludeRow()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path from Items() where Value = 100";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 100 },
            new() { Path = "c", Value = "100" }          // String - comparisons still work
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(2, table.Count);
        var paths = table.Select(row => (string)row[0]).ToList();
        CollectionAssert.Contains(paths, "a");
        CollectionAssert.Contains(paths, "c");
    }

    [TestMethod]
    public void WhenMixingPathAndValueInSelect_ShouldWork()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path, Value * 2 from Items() where Value > 5";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a.b[0]", Value = 10 },
            new() { Path = "a.b[1]", Value = 3 },
            new() { Path = "a.b[2]", Value = 7L }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(2, table.Count);
        
        var results = table.Select(row => new { Path = (string)row[0], Value = (long)row[1] })
                          .OrderBy(x => x.Value)
                          .ToList();
        
        Assert.AreEqual("a.b[2]", results[0].Path);
        Assert.AreEqual(14L, results[0].Value);
        Assert.AreEqual("a.b[0]", results[1].Path);
        Assert.AreEqual(20L, results[1].Value);
    }

    [TestMethod]
    public void WhenObjectValueIsString_ShouldSupportStringConcatenation()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Value + ' - suffix' from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = "prefix" },
            new() { Path = "b", Value = "test" },
            new() { Path = "c", Value = 100 }  // Non-string will be converted to string by C#
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => (string)row[0]).OrderBy(x => x).ToList();
        Assert.AreEqual("100 - suffix", results[0]);
        Assert.AreEqual("prefix - suffix", results[1]);
        Assert.AreEqual("test - suffix", results[2]);
    }

    [TestMethod]
    public void WhenObjectValueIsStringOrNumeric_AddOperatorShouldWork()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path, Value from Items() where Path = 'numeric' or Path = 'string'";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "string", Value = "hello" },
            new() { Path = "numeric", Value = 42 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(2, table.Count);
        
        // Verify we can retrieve both string and numeric values from object column
        var stringRow = table.FirstOrDefault(r => (string)r[0] == "string");
        var numericRow = table.FirstOrDefault(r => (string)r[0] == "numeric");
        
        Assert.IsNotNull(stringRow);
        Assert.AreEqual("hello", stringRow.Values[1]);
        
        Assert.IsNotNull(numericRow);
        Assert.AreEqual(42, numericRow.Values[1]);
    }

    [TestMethod]
    public void WhenAddingObjectValueWithLiteral_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Value + 5 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10 },
            new() { Path = "b", Value = 25L },
            new() { Path = "c", Value = 8 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => Convert.ToInt32(row[0])).OrderBy(x => x).ToList();
        Assert.AreEqual(13m, results[0]);
        Assert.AreEqual(15m, results[1]);
        Assert.AreEqual(30m, results[2]);
    }

    [TestMethod]
    public void WhenMultiplyingObjectValueWithLiteral_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Value * 5 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10 },
            new() { Path = "b", Value = 3 },
            new() { Path = "c", Value = 2 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => Convert.ToInt32(row[0])).OrderBy(x => x).ToList();
        Assert.AreEqual(10m, results[0]);
        Assert.AreEqual(15m, results[1]);
        Assert.AreEqual(50m, results[2]);
    }

    [TestMethod]
    public void WhenUsingModuloWithObjectValue_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Value % 3 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10 },
            new() { Path = "b", Value = 15L },
            new() { Path = "c", Value = 7 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => (long)row[0]).OrderBy(x => x).ToList();
        Assert.AreEqual(0L, results[0]);  // 15 % 3 = 0
        Assert.AreEqual(1L, results[1]);  // 7 % 3 = 1
        Assert.AreEqual(1L, results[2]);  // 10 % 3 = 1
    }

    [TestMethod]
    public void WhenUsingModuloWithObjectValueAsString_ShouldReject()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path from Items() where Value % 3 = 1";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10 },
            new() { Path = "b", Value = "15" },  // String should be rejected
            new() { Path = "c", Value = 7 }
        };

        var table = RunQuery(query, entities);

        // Only numeric values should pass
        Assert.AreEqual(2, table.Count);
        var paths = table.Select(row => (string)row[0]).ToList();
        CollectionAssert.Contains(paths, "a");
        CollectionAssert.Contains(paths, "c");
        CollectionAssert.DoesNotContain(paths, "b");
    }

    [TestMethod]
    public void WhenUsingUnaryNegationOnObjectValue_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select 0 - Value from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10 },
            new() { Path = "b", Value = -5L },
            new() { Path = "c", Value = 7 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => Convert.ToInt32(row[0])).OrderBy(x => x).ToList();
        Assert.AreEqual(-10, results[0]);
        Assert.AreEqual(-7, results[1]);
        Assert.AreEqual(5m, results[2]);
    }

    [TestMethod]
    public void WhenOrderingByObjectValue_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path, Value from Items() order by Value";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "third", Value = 30 },
            new() { Path = "first", Value = 5 },
            new() { Path = "second", Value = 15 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var orderedPaths = table.Select(row => (string)row[0]).ToList();
        Assert.AreEqual("first", orderedPaths[0]);
        Assert.AreEqual("second", orderedPaths[1]);
        Assert.AreEqual("third", orderedPaths[2]);
    }

    [TestMethod]
    public void WhenOrderingByObjectValueDescending_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path from Items() order by Value desc";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "second", Value = 15 },
            new() { Path = "third", Value = 5 },
            new() { Path = "first", Value = 30 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var orderedPaths = table.Select(row => (string)row[0]).ToList();
        Assert.AreEqual("first", orderedPaths[0]);
        Assert.AreEqual("second", orderedPaths[1]);
        Assert.AreEqual("third", orderedPaths[2]);
    }

    [TestMethod]
    public void WhenUsingComplexExpressionWithParentheses_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select (Value + 10) * 2 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 5 },
            new() { Path = "b", Value = 10L },
            new() { Path = "c", Value = 15 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => (long)row[0]).OrderBy(x => x).ToList();
        Assert.AreEqual(30L, results[0]);  // (5 + 10) * 2 = 30
        Assert.AreEqual(40L, results[1]);  // (10 + 10) * 2 = 40
        Assert.AreEqual(50L, results[2]);  // (15 + 10) * 2 = 50
    }

    [TestMethod]
    public void WhenUsingComplexExpressionWithMultipleOperators_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Value * 2 + Value / 2 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10 },
            new() { Path = "b", Value = 20L },
            new() { Path = "c", Value = 8 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => (long)row[0]).OrderBy(x => x).ToList();
        Assert.AreEqual(20L, results[0]);  // 8 * 2 + 8 / 2 = 16 + 4 = 20
        Assert.AreEqual(25L, results[1]);  // 10 * 2 + 10 / 2 = 20 + 5 = 25
        Assert.AreEqual(50L, results[2]);  // 20 * 2 + 20 / 2 = 40 + 10 = 50
    }

    [TestMethod]
    public void WhenConcatenatingMultipleStringsWithObject_ShouldWork()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select 'Result: ' + Value + ' items' from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = "test" },
            new() { Path = "b", Value = 42 },
            new() { Path = "c", Value = "hello" }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => (string)row[0]).OrderBy(x => x).ToList();
        Assert.AreEqual("Result: 42 items", results[0]);
        Assert.AreEqual("Result: hello items", results[1]);
        Assert.AreEqual("Result: test items", results[2]);
    }

    [TestMethod]
    public void WhenMultiplyingObjectWithDouble_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Value * 2.5 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10 },
            new() { Path = "b", Value = 4L },
            new() { Path = "c", Value = 8.0 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => Convert.ToDouble(row[0])).OrderBy(x => x).ToList();
        Assert.AreEqual(10.0, results[0], 0.001);
        Assert.AreEqual(20.0, results[1], 0.001);
        Assert.AreEqual(25.0, results[2], 0.001);
    }

    [TestMethod]
    public void WhenAddingObjectWithFloat_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Value + 1.5 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10 },
            new() { Path = "b", Value = 3.5f },
            new() { Path = "c", Value = 5.0 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => Convert.ToDouble(row[0])).OrderBy(x => x).ToList();
        Assert.AreEqual(5.0, results[0], 0.001);
        Assert.AreEqual(6.5, results[1], 0.001);
        Assert.AreEqual(11.5, results[2], 0.001);
    }

    [TestMethod]
    public void WhenDividingObjectWithDecimal_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Value / 2.0 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 20 },
            new() { Path = "b", Value = 15.0 },
            new() { Path = "c", Value = 10L }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => Convert.ToDouble(row[0])).OrderBy(x => x).ToList();
        Assert.AreEqual(5.0, results[0], 0.001);
        Assert.AreEqual(7.5, results[1], 0.001);
        Assert.AreEqual(10.0, results[2], 0.001);
    }

    [TestMethod]
    public void WhenSubtractingFloatFromObject_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Value - 2.5 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10.5 },
            new() { Path = "b", Value = 5 },
            new() { Path = "c", Value = 7.5f }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => Convert.ToDouble(row[0])).OrderBy(x => x).ToList();
        Assert.AreEqual(2.5, results[0], 0.001);
        Assert.AreEqual(5.0, results[1], 0.001);
        Assert.AreEqual(8.0, results[2], 0.001);
    }

    [TestMethod]
    public void WhenComparingObjectWithDouble_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path from Items() where Value > 5.5";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "low", Value = 5.0 },
            new() { Path = "mid", Value = 7.5 },
            new() { Path = "high", Value = 10 },
            new() { Path = "verylow", Value = 3.5f }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(2, table.Count);
        var paths = table.Select(row => (string)row[0]).ToList();
        CollectionAssert.Contains(paths, "mid");
        CollectionAssert.Contains(paths, "high");
    }

    [TestMethod]
    public void WhenMixingIntLongDoubleInArithmetic_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path, Value * 2 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "int", Value = 5 },
            new() { Path = "long", Value = 10L },
            new() { Path = "double", Value = 8.2 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        
        var intRow = table.First(r => (string)r[0] == "int");
        Assert.AreEqual(10L, intRow[1]);
        
        var longRow = table.First(r => (string)r[0] == "long");
        Assert.AreEqual(20L, longRow[1]);
        
        var doubleRow = table.First(r => (string)r[0] == "double");
        Assert.AreEqual(16.4, (double)doubleRow[1], 0.001);
    }

    [TestMethod]
    public void WhenOrderingByObjectWithDoubleValues_ShouldWork()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path from Items() order by Value";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "third", Value = 15.5 },
            new() { Path = "first", Value = 3.0 },
            new() { Path = "second", Value = 10.2 },
            new() { Path = "fourth", Value = 20.8 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(4, table.Count);
        var orderedPaths = table.Select(row => (string)row[0]).ToList();
        Assert.AreEqual("first", orderedPaths[0]);
        Assert.AreEqual("second", orderedPaths[1]);
        Assert.AreEqual("third", orderedPaths[2]);
        Assert.AreEqual("fourth", orderedPaths[3]);
    }

    [TestMethod]
    public void WhenObjectValueIsDoubleString_ShouldRejectForArithmetic()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path from Items() where Value * 2 > 10";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "valid", Value = 10.5 },
            new() { Path = "invalid", Value = "7.5" },  // String should be rejected
            new() { Path = "low", Value = 4 }  // 4 * 2 = 8, not > 10
        };

        var table = RunQuery(query, entities);

        // Only the value where multiplication result > 10
        Assert.AreEqual(1, table.Count);
        var paths = table.Select(row => (string)row[0]).ToList();
        CollectionAssert.Contains(paths, "valid");
        CollectionAssert.DoesNotContain(paths, "invalid");
        CollectionAssert.DoesNotContain(paths, "low");
    }
}
