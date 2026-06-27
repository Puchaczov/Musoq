using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.PathValue;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class AutomaticNumericTypeInferencePathValueTests : PathValueQueryTestBase
{
    [TestMethod]
    public void WhenSelectingObjectValueMultipliedBy2_ShouldAutoConvert()
    {
        const string query = "table Items {" +
                             "  Path: string," +
                             "  Value: object" +
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
                             "  Path: string," +
                             "  Value: object" +
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
                             "  Path: string," +
                             "  Value: object" +
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
                             "  Path: string," +
                             "  Value: object" +
                             "};" +
                             "couple #pathvalue.data with table Items as Items; " +
                             "select Path from Items() where Value * 2 > 100";

        var entities = new List<PathValueEntity>
        {
            new()
            {
                Path = "a.b", Value = "100"
            },
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
                             "  Path: string," +
                             "  Value: object" +
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
                             "  Path: string," +
                             "  Value: object" +
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
                             "  Path: string," +
                             "  Value: object" +
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
                             "  Path: string," +
                             "  Value: object" +
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
                             "  Path: string," +
                             "  Value: object" +
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
                             "  Path: string," +
                             "  Value: object" +
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
                             "  Path: string," +
                             "  Value: object" +
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
                             "  Path: string," +
                             "  Value: object" +
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

}
