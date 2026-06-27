using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.PathValue;

namespace Musoq.Evaluator.Tests;

public partial class AutomaticNumericTypeInferencePathValueTests
{
    [TestMethod]
    public void WhenCaseWhenReturnsMixedLongLiteralAndObjectValue_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: object" +
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
                             "  Path: string," +
                             "  Value: object" +
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
                             "  Path: string," +
                             "  Value: object" +
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
                             "  Path: string," +
                             "  Value: object" +
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
                             "  Path: string," +
                             "  Value: object" +
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
                             "  Path: string," +
                             "  Value: object" +
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
                             "  Path: string," +
                             "  Value: object" +
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
