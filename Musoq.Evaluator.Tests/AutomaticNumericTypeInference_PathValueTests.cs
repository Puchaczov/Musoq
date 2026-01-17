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
            new() { Path = "a.b", Value = "100" },
            new() { Path = "a.c", Value = 100 },
            new() { Path = "a.d", Value = 200L }
        };

        var table = RunQuery(query, entities);


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
            new() { Path = "d", Value = "8" },
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
            new() { Path = "c", Value = "42" },
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
            new() { Path = "c", Value = 15 },
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
            new() { Path = "c", Value = "100" }
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
            new() { Path = "c", Value = 100 }
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
        Assert.AreEqual(0L, results[0]);
        Assert.AreEqual(1L, results[1]);
        Assert.AreEqual(1L, results[2]);
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
            new() { Path = "b", Value = "15" },
            new() { Path = "c", Value = 7 }
        };

        var table = RunQuery(query, entities);


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
        Assert.AreEqual(30L, results[0]);
        Assert.AreEqual(40L, results[1]);
        Assert.AreEqual(50L, results[2]);
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
        Assert.AreEqual(20L, results[0]);
        Assert.AreEqual(25L, results[1]);
        Assert.AreEqual(50L, results[2]);
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
            new() { Path = "invalid", Value = "7.5" },
            new() { Path = "low", Value = 4 }
        };

        var table = RunQuery(query, entities);


        Assert.AreEqual(1, table.Count);
        var paths = table.Select(row => (string)row[0]).ToList();
        CollectionAssert.Contains(paths, "valid");
        CollectionAssert.DoesNotContain(paths, "invalid");
        CollectionAssert.DoesNotContain(paths, "low");
    }

    [TestMethod]
    public void WhenCaseWhenReturnsMixedIntLiteralAndObjectValue_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path, CASE WHEN Path = 'age' THEN 35 ELSE Value END as Value from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "age", Value = 30 },
            new() { Path = "name", Value = "John" },
            new() { Path = "score", Value = 95 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);

        var ageRow = table.First(row => (string)row[0] == "age");
        Assert.AreEqual(35, Convert.ToInt32(ageRow[1]));

        var nameRow = table.First(row => (string)row[0] == "name");
        Assert.AreEqual("John", nameRow[1]);

        var scoreRow = table.First(row => (string)row[0] == "score");
        Assert.AreEqual(95, Convert.ToInt32(scoreRow[1]));
    }

    [TestMethod]
    public void WhenCaseWhenReturnsMixedLongLiteralAndObjectValue_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path, CASE WHEN Path = 'large' THEN 9223372036854775807l ELSE Value END as Value from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "large", Value = 100L },
            new() { Path = "text", Value = "Data" },
            new() { Path = "small", Value = 42 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);

        var largeRow = table.First(row => (string)row[0] == "large");
        Assert.AreEqual(9223372036854775807L, Convert.ToInt64(largeRow[1]));

        var textRow = table.First(row => (string)row[0] == "text");
        Assert.AreEqual("Data", textRow[1]);

        var smallRow = table.First(row => (string)row[0] == "small");
        Assert.AreEqual(42, Convert.ToInt32(smallRow[1]));
    }

    [TestMethod]
    public void WhenCaseWhenReturnsMixedDecimalLiteralAndObjectValue_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path, CASE WHEN Path = 'price' THEN 99.99 ELSE Value END as Value from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "price", Value = 50.00m },
            new() { Path = "name", Value = "Product" },
            new() { Path = "quantity", Value = 10 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);

        var priceRow = table.First(row => (string)row[0] == "price");
        Assert.AreEqual(99.99m, Convert.ToDecimal(priceRow[1]));

        var nameRow = table.First(row => (string)row[0] == "name");
        Assert.AreEqual("Product", nameRow[1]);

        var quantityRow = table.First(row => (string)row[0] == "quantity");
        Assert.AreEqual(10, Convert.ToInt32(quantityRow[1]));
    }

    [TestMethod]
    public void WhenCaseWhenReturnsMixedStringLiteralAndObjectValue_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path, CASE WHEN Path = 'status' THEN 'Active' ELSE Value END as Value from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "status", Value = "Inactive" },
            new() { Path = "count", Value = 42 },
            new() { Path = "amount", Value = 123.45m }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(3, table.Count);

        var statusRow = table.First(row => (string)row[0] == "status");
        Assert.AreEqual("Active", statusRow[1]);

        var countRow = table.First(row => (string)row[0] == "count");
        Assert.AreEqual(42, Convert.ToInt32(countRow[1]));

        var amountRow = table.First(row => (string)row[0] == "amount");
        Assert.AreEqual(123.45m, Convert.ToDecimal(amountRow[1]));
    }

    [TestMethod]
    public void WhenCaseWhenReturnsMultipleBranchesMixedWithObject_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path, CASE " +
                             "  WHEN Path = 'int' THEN 100 " +
                             "  WHEN Path = 'string' THEN 'Text' " +
                             "  ELSE Value END as Value from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "int", Value = 0 },
            new() { Path = "string", Value = "Original" },
            new() { Path = "decimal", Value = 99.99m },
            new() { Path = "other", Value = true }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(4, table.Count);

        var intRow = table.First(row => (string)row[0] == "int");
        Assert.AreEqual(100, Convert.ToInt32(intRow[1]));

        var stringRow = table.First(row => (string)row[0] == "string");
        Assert.AreEqual("Text", stringRow[1]);

        var decimalRow = table.First(row => (string)row[0] == "decimal");
        Assert.AreEqual(99.99m, Convert.ToDecimal(decimalRow[1]));

        var otherRow = table.First(row => (string)row[0] == "other");
        Assert.IsTrue(Convert.ToBoolean(otherRow[1]));
    }

    [TestMethod]
    public void WhenCaseWhenReturnsIntAndLong_ShouldPromoteToLong()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path, CASE WHEN Path = 'big' THEN 9223372036854775807l ELSE 42 END as Value from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "big", Value = 0L },
            new() { Path = "small", Value = 0 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(2, table.Count);

        var bigRow = table.First(row => (string)row[0] == "big");
        Assert.AreEqual(9223372036854775807L, Convert.ToInt64(bigRow[1]));

        var smallRow = table.First(row => (string)row[0] == "small");
        Assert.AreEqual(42L, Convert.ToInt64(smallRow[1]));
    }

    [TestMethod]
    public void WhenCaseWhenReturnsIntAndDecimal_ShouldPromoteToDecimal()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path, CASE WHEN Path = 'precise' THEN 99.99 ELSE 42 END as Value from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "precise", Value = 0m },
            new() { Path = "whole", Value = 0 }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(2, table.Count);

        var preciseRow = table.First(row => (string)row[0] == "precise");
        Assert.AreEqual(99.99m, Convert.ToDecimal(preciseRow[1]));

        var wholeRow = table.First(row => (string)row[0] == "whole");
        Assert.AreEqual(42m, Convert.ToDecimal(wholeRow[1]));
    }

    [TestMethod]
    public void WhenCaseWhenReturnsStringAndInt_ShouldPromoteToObject()
    {
        const string query = "table Items {" +
                             "  Path 'System.String'," +
                             "  Value 'System.Object'" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path, CASE WHEN Path = 'text' THEN 'Hello' ELSE 42 END as Value from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "text", Value = null },
            new() { Path = "number", Value = null }
        };

        var table = RunQuery(query, entities);

        Assert.AreEqual(2, table.Count);

        var textRow = table.First(row => (string)row[0] == "text");
        Assert.AreEqual("Hello", textRow[1]);

        var numberRow = table.First(row => (string)row[0] == "number");
        Assert.AreEqual(42, Convert.ToInt32(numberRow[1]));
    }
}