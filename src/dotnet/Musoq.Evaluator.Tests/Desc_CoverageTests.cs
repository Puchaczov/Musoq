using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Dynamic;
using Musoq.Plugins;
using Musoq.Schema.Helpers;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tests;

public partial class DescStatementTests
{
    #region Additional Coverage Tests

    [TestMethod]
    public void DescSchema_WithSemicolon_ShouldWork()
    {
        var query = "desc #A;";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(1, table.Columns.Count(), "Should have exactly 1 column");
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnIndex);


        Assert.IsGreaterThan(0, table.Count, "Should return at least one method");
        var methodNames = table.Select(row => (string)row[0]).ToList();
        Assert.Contains("empty", methodNames, "Should contain 'empty' method");
        Assert.Contains("entities", methodNames, "Should contain 'entities' method");
    }

    [TestMethod]
    public void DescSchema_CaseInsensitive_ShouldWork()
    {
        var query = "DESC #A";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(1, table.Columns.Count(), "Should have exactly 1 column");
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);


        Assert.IsGreaterThan(0, table.Count, "Should return methods");
        var methodNames = table.Select(row => (string)row[0]).ToList();
        Assert.Contains("empty", methodNames, "Should contain 'empty' method");
        Assert.Contains("entities", methodNames, "Should contain 'entities' method");
    }

    [TestMethod]
    public void DescSchema_HashOptional_ShouldWork()
    {
        var query = "desc A";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(1, table.Columns.Count(), "Should have exactly 1 column");
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);


        Assert.IsGreaterThan(0, table.Count, "Should return methods");
        var methodNames = table.Select(row => (string)row[0]).ToList();
        Assert.Contains("empty", methodNames, "Should contain 'empty' method");
        Assert.Contains("entities", methodNames, "Should contain 'entities' method");
    }

    [TestMethod]
    public void DescSchema_OutputStructure_ShouldBeCorrect()
    {
        var query = "desc #A";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(1, table.Columns.Count(), "Should have exactly 1 column");
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName, "Column should be named 'Name'");
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType, "Column should be string type");
        Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnIndex, "Column index should be 0");
    }

    [TestMethod]
    public void DescSchemaMethod_OutputStructure_ShouldBeCorrect()
    {
        var query = "desc #A.entities";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.IsGreaterThanOrEqualTo(1, table.Columns.Count(), "Should have at least Name column");
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName, "First column should be 'Name'");
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType, "Name column should be string");


        if (table.Columns.Count() > 1)
            for (var i = 1; i < table.Columns.Count(); i++)
            {
                Assert.AreEqual($"Param {i - 1}", table.Columns.ElementAt(i).ColumnName,
                    $"Column {i} should be named 'Param {i - 1}'");
                Assert.AreEqual(typeof(string), table.Columns.ElementAt(i).ColumnType,
                    $"Param {i - 1} column should be string type");
            }
    }

    [TestMethod]
    public void DescSchemaMethod_WithSemicolon_ShouldWork()
    {
        var query = "desc #A.entities;";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.IsGreaterThanOrEqualTo(1, table.Columns.Count(), "Should have at least Name column");
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnIndex);


        Assert.AreEqual(1, table.Count, "Should return exactly one method overload");
        Assert.AreEqual("entities", (string)table[0][0], "Should return 'entities' method");
    }

    [TestMethod]
    public void DescSchemaMethod_CaseInsensitive_ShouldWork()
    {
        // Note: Method names are case-sensitive in schema registration

        var query = "DESC #A.entities";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Should return method constructor information");
        Assert.AreEqual("entities", (string)table[0][0], "DESC keyword should be case insensitive");
    }

    [TestMethod]
    public void DescSchemaMethod_HashOptional_ShouldWork()
    {
        var query = "desc A.entities";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.IsGreaterThanOrEqualTo(1, table.Columns.Count(), "Should have at least Name column");
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);


        Assert.AreEqual(1, table.Count, "Should return exactly one method overload");
        Assert.AreEqual("entities", (string)table[0][0], "Should return 'entities' method");
    }

    [TestMethod]
    public void DescSchemaMethodWithParentheses_OutputStructure_ShouldBeCorrect()
    {
        var query = "desc #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(3, table.Columns.Count(), "Should have exactly 3 columns");

        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName, "First column should be 'Name'");
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType, "Name column should be string");
        Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnIndex, "Name column index should be 0");

        Assert.AreEqual("Index", table.Columns.ElementAt(1).ColumnName, "Second column should be 'Index'");
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType, "Index column should be int");
        Assert.AreEqual(1, table.Columns.ElementAt(1).ColumnIndex, "Index column index should be 1");

        Assert.AreEqual("Type", table.Columns.ElementAt(2).ColumnName, "Third column should be 'Type'");
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType, "Type column should be string");
        Assert.AreEqual(2, table.Columns.ElementAt(2).ColumnIndex, "Type column index should be 2");
    }

    [TestMethod]
    public void DescSchemaMethodWithParentheses_HashOptional_ShouldWork()
    {
        var query = "desc A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(3, table.Columns.Count(), "Should have exactly 3 columns");
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Index", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("Type", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);


        Assert.IsGreaterThan(0, table.Count, "Should return column information");
        var columnNames = table.Select(row => (string)row[0]).ToList();
        Assert.Contains("Name", columnNames, "Should contain 'Name' column");
        Assert.Contains("City", columnNames, "Should contain 'City' column");
        Assert.Contains("Country", columnNames, "Should contain 'Country' column");
    }

    [TestMethod]
    public void DescFunctionsSchema_OutputStructure_ShouldBeCorrect()
    {
        var query = "desc functions #A";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(4, table.Columns.Count(), "Should have exactly 4 columns");

        Assert.AreEqual("Method", table.Columns.ElementAt(0).ColumnName, "First column should be 'Method'");
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType, "Method column should be string");
        Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnIndex, "Method column index should be 0");

        Assert.AreEqual("Description", table.Columns.ElementAt(1).ColumnName, "Second column should be 'Description'");
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType, "Description column should be string");
        Assert.AreEqual(1, table.Columns.ElementAt(1).ColumnIndex, "Description column index should be 1");

        Assert.AreEqual("Category", table.Columns.ElementAt(2).ColumnName, "Third column should be 'Category'");
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType, "Category column should be string");
        Assert.AreEqual(2, table.Columns.ElementAt(2).ColumnIndex, "Category column index should be 2");

        Assert.AreEqual("Source", table.Columns.ElementAt(3).ColumnName, "Fourth column should be 'Source'");
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(3).ColumnType, "Source column should be string");
        Assert.AreEqual(3, table.Columns.ElementAt(3).ColumnIndex, "Source column index should be 3");
    }

    [TestMethod]
    public void DescFunctionsSchema_HashOptional_ShouldWork()
    {
        var query = "desc functions A";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(4, table.Columns.Count(), "Should have exactly 4 columns");
        Assert.AreEqual("Method", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnIndex);
        Assert.AreEqual("Description", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual(1, table.Columns.ElementAt(1).ColumnIndex);
        Assert.AreEqual("Category", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual(2, table.Columns.ElementAt(2).ColumnIndex);
        Assert.AreEqual("Source", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(3).ColumnType);
        Assert.AreEqual(3, table.Columns.ElementAt(3).ColumnIndex);


        Assert.IsGreaterThan(0, table.Count, "Should return library methods");
        var methodSignatures = table.Select(row => (string)row[0]).ToList();
        Assert.IsTrue(methodSignatures.Any(m => m.Contains("Trim(")), "Should contain Trim method");
        Assert.IsTrue(methodSignatures.Any(m => m.Contains("Substring(")), "Should contain Substring method");


        foreach (var row in table)
        {
            Assert.IsNotNull(row[0], "Method should not be null");
            Assert.IsNotNull(row[1], "Description should not be null");
            Assert.IsNotNull(row[2], "Category should not be null");
            Assert.IsNotNull(row[3], "Source should not be null");
            var source = (string)row[3];
            Assert.IsTrue(source == "Library" || source == "Schema",
                $"Source should be either 'Library' or 'Schema', got: {source}");
        }
    }

    [TestMethod]
    public void DescFunctionsSchema_SortOrder_ShouldBeCorrect()
    {
        var query = "desc functions #A";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        var allRows = table.Select(row => new
        {
            Method = (string)row[0],
            Category = (string)row[2],
            Source = (string)row[3]
        }).ToList();

        // Note: BasicSchema only has Library methods, no Schema-specific methods
        var libraryRows = allRows.Where(r => r.Source == "Library").ToList();


        var categories = libraryRows.Select(r => r.Category).Distinct().ToList();
        foreach (var category in categories)
        {
            var categoryMethods = libraryRows.Where(r => r.Category == category).ToList();
            var firstIndex = libraryRows.IndexOf(categoryMethods.First());
            var lastIndex = libraryRows.LastIndexOf(categoryMethods.Last());
            var countInRange = lastIndex - firstIndex + 1;

            Assert.AreEqual(categoryMethods.Count, countInRange,
                $"All methods from category '{category}' should appear consecutively (found {categoryMethods.Count} methods spread across {countInRange} positions)");
        }


        for (var i = 1; i < categories.Count; i++)
            Assert.IsLessThanOrEqualTo(0,
                string.Compare(categories[i - 1], categories[i], StringComparison.Ordinal),
                $"Category '{categories[i - 1]}' should come before or equal to '{categories[i]}'");
    }

    [TestMethod]
    public void DescSchemaMethodColumn_OutputStructure_ShouldBeCorrect()
    {
        var query = "desc #A.entities() column Array";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);


        Assert.AreEqual(3, table.Columns.Count(), "Should have exactly 3 columns");

        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName, "First column should be 'Name'");
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType, "Name column should be string");
        Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnIndex, "Name column index should be 0");

        Assert.AreEqual("Index", table.Columns.ElementAt(1).ColumnName, "Second column should be 'Index'");
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType, "Index column should be int");
        Assert.AreEqual(1, table.Columns.ElementAt(1).ColumnIndex, "Index column index should be 1");

        Assert.AreEqual("Type", table.Columns.ElementAt(2).ColumnName, "Third column should be 'Type'");
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType, "Type column should be string");
        Assert.AreEqual(2, table.Columns.ElementAt(2).ColumnIndex, "Type column index should be 2");
    }

    #endregion
}
