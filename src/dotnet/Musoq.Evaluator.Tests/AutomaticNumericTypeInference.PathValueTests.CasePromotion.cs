using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.PathValue;

namespace Musoq.Evaluator.Tests;

public partial class AutomaticNumericTypeInferencePathValueTests
{
    [TestMethod]
    public void WhenUsingComplexExpressionWithParentheses_ShouldPreserveTypedResults()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: long?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select (Value + 10) * 2 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 5 },
            new() { Path = "b", Value = 10L },
            new() { Path = "c", Value = 15 }
        };

        var table = RunNativeNumericQuery<long>(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => (long)row.ReadValue(0)).OrderBy(x => x).ToList();
        Assert.AreEqual(30L, results[0]);
        Assert.AreEqual(40L, results[1]);
        Assert.AreEqual(50L, results[2]);
    }

    [TestMethod]
    public void WhenUsingComplexExpressionWithMultipleOperators_ShouldPreserveTypedResults()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: long?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Value * 2 + Value / 2 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10 },
            new() { Path = "b", Value = 20L },
            new() { Path = "c", Value = 8 }
        };

        var table = RunNativeNumericQuery<long>(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => (long)row.ReadValue(0)).OrderBy(x => x).ToList();
        Assert.AreEqual(20L, results[0]);
        Assert.AreEqual(25L, results[1]);
        Assert.AreEqual(50L, results[2]);
    }

    [TestMethod]
    public void WhenConcatenatingMultipleStringsWithDecodedValue_ShouldWork()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: string" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select 'Result: ' + Value + ' items' from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = "test" },
            new() { Path = "b", Value = 42 },
            new() { Path = "c", Value = "hello" }
        };

        var table = RunNativeStringQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => (string)row.ReadValue(0)).OrderBy(x => x).ToList();
        Assert.AreEqual("Result: 42 items", results[0]);
        Assert.AreEqual("Result: hello items", results[1]);
        Assert.AreEqual("Result: test items", results[2]);
    }

    [TestMethod]
    public void WhenMultiplyingTypedNumericWithDouble_ShouldPreserveTypedResults()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: decimal?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Value * 2.5 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10 },
            new() { Path = "b", Value = 4L },
            new() { Path = "c", Value = 8.0 }
        };

        var table = RunNativeNumericQuery<decimal>(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => Convert.ToDouble(row.ReadValue(0))).OrderBy(x => x).ToList();
        Assert.AreEqual(10.0, results[0], 0.001);
        Assert.AreEqual(20.0, results[1], 0.001);
        Assert.AreEqual(25.0, results[2], 0.001);
    }

    [TestMethod]
    public void WhenAddingTypedNumericWithFloat_ShouldPreserveTypedResults()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: decimal?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Value + 1.5 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10 },
            new() { Path = "b", Value = 3.5f },
            new() { Path = "c", Value = 5.0 }
        };

        var table = RunNativeNumericQuery<decimal>(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => Convert.ToDouble(row.ReadValue(0))).OrderBy(x => x).ToList();
        Assert.AreEqual(5.0, results[0], 0.001);
        Assert.AreEqual(6.5, results[1], 0.001);
        Assert.AreEqual(11.5, results[2], 0.001);
    }

    [TestMethod]
    public void WhenDividingTypedNumericWithDecimal_ShouldPreserveTypedResults()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: decimal?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Value / 2.0 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 20 },
            new() { Path = "b", Value = 15.0 },
            new() { Path = "c", Value = 10L }
        };

        var table = RunNativeNumericQuery<decimal>(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => Convert.ToDouble(row.ReadValue(0))).OrderBy(x => x).ToList();
        Assert.AreEqual(5.0, results[0], 0.001);
        Assert.AreEqual(7.5, results[1], 0.001);
        Assert.AreEqual(10.0, results[2], 0.001);
    }

    [TestMethod]
    public void WhenSubtractingFloatFromTypedNumeric_ShouldPreserveTypedResults()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: decimal?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Value - 2.5 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10.5 },
            new() { Path = "b", Value = 5 },
            new() { Path = "c", Value = 7.5f }
        };

        var table = RunNativeNumericQuery<decimal>(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => Convert.ToDouble(row.ReadValue(0))).OrderBy(x => x).ToList();
        Assert.AreEqual(2.5, results[0], 0.001);
        Assert.AreEqual(5.0, results[1], 0.001);
        Assert.AreEqual(8.0, results[2], 0.001);
    }

    [TestMethod]
    public void WhenComparingDecodedNumericWithDouble_ShouldPreserveTypedResults()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: decimal?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Path from Items() where Value > 5.5";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "low", Value = 5.0 },
            new() { Path = "mid", Value = 7.5 },
            new() { Path = "high", Value = 10 },
            new() { Path = "verylow", Value = 3.5f }
        };

        var table = RunNativeNumericQuery<decimal>(query, entities);

        Assert.AreEqual(2, table.Count);
        var paths = table.Select(row => (string)row.ReadValue(0)).ToList();
        CollectionAssert.Contains(paths, "mid");
        CollectionAssert.Contains(paths, "high");
    }

    [TestMethod]
    public void WhenOrderingByDecodedNumericWithDoubleValues_ShouldWork()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: double?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Path from Items() order by Value";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "third", Value = 15.5 },
            new() { Path = "first", Value = 3.0 },
            new() { Path = "second", Value = 10.2 },
            new() { Path = "fourth", Value = 20.8 }
        };

        var table = RunNativeNumericQuery<double>(query, entities);

        Assert.AreEqual(4, table.Count);
        var orderedPaths = table.Select(row => (string)row.ReadValue(0)).ToList();
        Assert.AreEqual("first", orderedPaths[0]);
        Assert.AreEqual("second", orderedPaths[1]);
        Assert.AreEqual("third", orderedPaths[2]);
        Assert.AreEqual("fourth", orderedPaths[3]);
    }

    [TestMethod]
    public void WhenDecodedValueValueIsDoubleString_ShouldRejectForArithmetic()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: long?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Path from Items() where Value * 2 > 10";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "valid", Value = 10.5 },
            new() { Path = "invalid", Value = "7.5" },
            new() { Path = "low", Value = 4 }
        };

        var table = RunNativeNumericQuery<long>(query, entities, allowNumericText: false);


        Assert.AreEqual(1, table.Count);
        var paths = table.Select(row => (string)row.ReadValue(0)).ToList();
        CollectionAssert.Contains(paths, "valid");
        CollectionAssert.DoesNotContain(paths, "invalid");
        CollectionAssert.DoesNotContain(paths, "low");
    }

    [TestMethod]
    public void WhenCaseWhenReturnsMixedIntLiteralAndTypedColumnsValue_ShouldPreserveScenarioValues()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Integral: long?, Amount: decimal?, Text: string, Flag: bool?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Path, CASE WHEN Path = 'age' THEN 35 ELSE Integral END as Value, Text from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "age", Value = 30 },
            new() { Path = "name", Value = "John" },
            new() { Path = "score", Value = 95 }
        };

        var table = RunNativeMixedQuery(query, entities);

        Assert.AreEqual(3, table.Count);

        var ageRow = table.First(row => (string)row.ReadValue(0) == "age");
        Assert.AreEqual(35, Convert.ToInt32(ageRow.ReadValue(1)));

        var nameRow = table.First(row => (string)row.ReadValue(0) == "name");
        Assert.AreEqual("John", nameRow.GetValue<string?>(2));

        var scoreRow = table.First(row => (string)row.ReadValue(0) == "score");
        Assert.AreEqual(95, Convert.ToInt32(scoreRow.ReadValue(1)));
    }

}
