using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.PathValue;

namespace Musoq.Evaluator.Tests;

public partial class AutomaticNumericTypeInferencePathValueTests
{
    [TestMethod]
    public void WhenUsingComplexExpression_ShouldPreserveTypedResults()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: long?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select (Value * 2) + 10 from Items() where Value >= 5 and Value <= 15";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 5 },
            new() { Path = "b", Value = 10L },
            new() { Path = "c", Value = 15 },
            new() { Path = "d", Value = 3 },
            new() { Path = "e", Value = 20 }
        };

        var table = RunNativeNumericQuery<long>(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => (long)row.ReadValue(0)).OrderBy(x => x).ToList();
        Assert.AreEqual(20L, results[0]);
        Assert.AreEqual(30L, results[1]);
        Assert.AreEqual(40L, results[2]);
    }

    [TestMethod]
    public void WhenDecodedValueValueOverflowsInt32_ShouldExcludeRow()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: long?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Path from Items() where Value = 100";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 100 },
            new() { Path = "c", Value = "100" }
        };

        var table = RunNativeNumericQuery<long>(query, entities, allowNumericText: true);

        Assert.AreEqual(2, table.Count);
        var paths = table.Select(row => (string)row.ReadValue(0)).ToList();
        CollectionAssert.Contains(paths, "a");
        CollectionAssert.Contains(paths, "c");
    }

    [TestMethod]
    public void WhenMixingPathAndValueInSelect_ShouldWork()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: long?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Path, Value * 2 from Items() where Value > 5";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a.b[0]", Value = 10 },
            new() { Path = "a.b[1]", Value = 3 },
            new() { Path = "a.b[2]", Value = 7L }
        };

        var table = RunNativeNumericQuery<long>(query, entities);

        Assert.AreEqual(2, table.Count);

        var results = table.Select(row => new { Path = (string)row.ReadValue(0), Value = (long)row.ReadValue(1) })
            .OrderBy(x => x.Value)
            .ToList();

        Assert.AreEqual("a.b[2]", results[0].Path);
        Assert.AreEqual(14L, results[0].Value);
        Assert.AreEqual("a.b[0]", results[1].Path);
        Assert.AreEqual(20L, results[1].Value);
    }

    [TestMethod]
    public void WhenDecodedValueValueIsString_ShouldSupportStringConcatenation()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: string" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Value + ' - suffix' from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = "prefix" },
            new() { Path = "b", Value = "test" },
            new() { Path = "c", Value = 100 }
        };

        var table = RunNativeStringQuery(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => (string)row.ReadValue(0)).OrderBy(x => x).ToList();
        Assert.AreEqual("100 - suffix", results[0]);
        Assert.AreEqual("prefix - suffix", results[1]);
        Assert.AreEqual("test - suffix", results[2]);
    }

    [TestMethod]
    public void WhenTypedColumnsValueIsStringOrNumeric_AddOperatorShouldWork()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Integral: long?, Amount: decimal?, Text: string, Flag: bool?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Path, Integral, Text from Items() where Path = 'numeric' or Path = 'string'";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "string", Value = "hello" },
            new() { Path = "numeric", Value = 42 }
        };

        var table = RunNativeMixedQuery(query, entities);

        Assert.AreEqual(2, table.Count);


        var stringRow = table.FirstOrDefault(r => (string)r.ReadValue(0) == "string");
        var numericRow = table.FirstOrDefault(r => (string)r.ReadValue(0) == "numeric");

        Assert.IsNotNull(stringRow);
        Assert.AreEqual("hello", stringRow.GetValue<string?>(2));

        Assert.IsNotNull(numericRow);
        Assert.AreEqual(42L, numericRow.GetValue<long?>(1));
    }

    [TestMethod]
    public void WhenAddingTypedNumericValueWithLiteral_ShouldPreserveTypedResults()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: long?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Value + 5 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10 },
            new() { Path = "b", Value = 25L },
            new() { Path = "c", Value = 8 }
        };

        var table = RunNativeNumericQuery<long>(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => Convert.ToInt32(row.ReadValue(0))).OrderBy(x => x).ToList();
        Assert.AreEqual(13m, results[0]);
        Assert.AreEqual(15m, results[1]);
        Assert.AreEqual(30m, results[2]);
    }

    [TestMethod]
    public void WhenMultiplyingTypedNumericValueWithLiteral_ShouldPreserveTypedResults()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: long?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Value * 5 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10 },
            new() { Path = "b", Value = 3 },
            new() { Path = "c", Value = 2 }
        };

        var table = RunNativeNumericQuery<long>(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => Convert.ToInt32(row.ReadValue(0))).OrderBy(x => x).ToList();
        Assert.AreEqual(10m, results[0]);
        Assert.AreEqual(15m, results[1]);
        Assert.AreEqual(50m, results[2]);
    }

    [TestMethod]
    public void WhenUsingModuloWithTypedNumericValue_ShouldPreserveTypedResults()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: long?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Value % 3 from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10 },
            new() { Path = "b", Value = 15L },
            new() { Path = "c", Value = 7 }
        };

        var table = RunNativeNumericQuery<long>(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => (long)row.ReadValue(0)).OrderBy(x => x).ToList();
        Assert.AreEqual(0L, results[0]);
        Assert.AreEqual(1L, results[1]);
        Assert.AreEqual(1L, results[2]);
    }

    [TestMethod]
    public void WhenUsingModuloWithDecodedNumericValueAsString_ShouldReject()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: long?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Path from Items() where Value % 3 = 1";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10 },
            new() { Path = "b", Value = "15" },
            new() { Path = "c", Value = 7 }
        };

        var table = RunNativeNumericQuery<long>(query, entities);


        Assert.AreEqual(2, table.Count);
        var paths = table.Select(row => (string)row.ReadValue(0)).ToList();
        CollectionAssert.Contains(paths, "a");
        CollectionAssert.Contains(paths, "c");
        CollectionAssert.DoesNotContain(paths, "b");
    }

    [TestMethod]
    public void WhenUsingUnaryNegationOnDecodedValueValue_ShouldKeepTypedResults()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: long?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select 0 - Value from Items()";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "a", Value = 10 },
            new() { Path = "b", Value = -5L },
            new() { Path = "c", Value = 7 }
        };

        var table = RunNativeNumericQuery<long>(query, entities);

        Assert.AreEqual(3, table.Count);
        var results = table.Select(row => Convert.ToInt32(row.ReadValue(0))).OrderBy(x => x).ToList();
        Assert.AreEqual(-10, results[0]);
        Assert.AreEqual(-7, results[1]);
        Assert.AreEqual(5m, results[2]);
    }

    [TestMethod]
    public void WhenOrderingByDecodedValueValue_ShouldKeepTypedResults()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: long?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Path, Value from Items() order by Value";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "third", Value = 30 },
            new() { Path = "first", Value = 5 },
            new() { Path = "second", Value = 15 }
        };

        var table = RunNativeNumericQuery<long>(query, entities);

        Assert.AreEqual(3, table.Count);
        var orderedPaths = table.Select(row => (string)row.ReadValue(0)).ToList();
        Assert.AreEqual("first", orderedPaths[0]);
        Assert.AreEqual("second", orderedPaths[1]);
        Assert.AreEqual("third", orderedPaths[2]);
    }

    [TestMethod]
    public void WhenOrderingByDecodedValueValueDescending_ShouldKeepTypedResults()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: long?" +
                             "};" +
                             "couple #schema.first with table Items as Items; " +
                             "select Path from Items() order by Value desc";

        var entities = new List<PathValueEntity>
        {
            new() { Path = "second", Value = 15 },
            new() { Path = "third", Value = 5 },
            new() { Path = "first", Value = 30 }
        };

        var table = RunNativeNumericQuery<long>(query, entities);

        Assert.AreEqual(3, table.Count);
        var orderedPaths = table.Select(row => (string)row.ReadValue(0)).ToList();
        Assert.AreEqual("first", orderedPaths[0]);
        Assert.AreEqual("second", orderedPaths[1]);
        Assert.AreEqual("third", orderedPaths[2]);
    }

}
