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
    #region Desc Column Tests

    [TestMethod]
    public void DescSchemaMethodColumn_Debug_ShouldShowGeneratedCode()
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

        var items = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver);

        var sourceCode = items.Compilation?.SyntaxTrees.ElementAt(0).GetRoot().ToFullString();
        TestContext.WriteLine("Generated source code:");
        TestContext.WriteLine(sourceCode);

        Assert.IsNotNull(sourceCode, "Source code should be generated");
        Assert.Contains("GetSpecificColumnDescription", sourceCode, "Should call GetSpecificColumnDescription");
    }

    [TestMethod]
    public void DescSchemaMethodColumn_ArrayOfPrimitives_ShouldReturnElementTypeInfo()
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

        Assert.AreEqual(3, table.Columns.Count(), "Should have 3 columns: Name, Index, Type");
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Index", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual("Type", table.Columns.ElementAt(2).ColumnName);

        Assert.AreEqual(1, table.Count, "Should return one row for primitive array element type");

        var columnName = (string)table[0][0];
        Assert.AreEqual("Array", columnName, "First row should be the Array column");

        var typeName = (string)table[0][2];
        Assert.Contains("Int32", typeName, $"Array element should be Int32 type, got: {typeName}");
    }

    [TestMethod]
    public void DescSchemaMethodColumn_CaseInsensitive_ShouldWork()
    {
        var query = "desc #A.entities() column array";

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

        Assert.AreEqual(1, table.Count, "Should find column with case-insensitive match");

        var columnName = (string)table[0][0];
        Assert.AreEqual("Array", columnName, "Should return the actual column name (Array)");
    }

    [TestMethod]
    public void DescSchemaMethodColumn_ComplexArrayType_ShouldReturnNestedProperties()
    {
        var query = "desc #A.entities() column Children";

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

        Assert.IsGreaterThan(1, table.Count,
            "Should return multiple rows for complex array element type with nested properties");

        var columnNames = table.Select(row => (string)row[0]).ToList();

        Assert.Contains("Children", columnNames, "Should contain the Children column itself");
        Assert.IsTrue(columnNames.Any(n => n.Contains(".")),
            "Should contain nested properties like Name, Id from BasicEntity");
    }

    [TestMethod]
    public void DescSchemaMethodColumn_NotFound_ShouldThrowException()
    {
        var query = "desc #A.entities() column NonExistentColumn";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var exception = Assert.Throws<UnknownColumnOrAliasException>(() => vm.Run(TestContext.CancellationToken));
        Assert.Contains("NonExistentColumn", exception.Message, "Exception message should contain the column name");
    }

    [TestMethod]
    public void DescSchemaMethodColumn_NonArrayColumn_ShouldThrowException()
    {
        var query = "desc #A.entities() column Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);


        Assert.Throws<ColumnMustBeAnArrayOrImplementIEnumerableException>(() => vm.Run(TestContext.CancellationToken));
    }

    [TestMethod]
    public void DescSchemaMethodColumn_NonArrayComplexType_ShouldDescribeType()
    {
        var query = "desc #A.entities() column Self";

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


        Assert.IsGreaterThan(0, table.Count, "Complex type should show its properties");
        var columnNames = table.Select(row => (string)row[0]).ToList();
        Assert.IsTrue(columnNames.Any(c => c.Equals("Children", StringComparison.OrdinalIgnoreCase)),
            "Should show Children property for exploratory navigation");
    }

    [TestMethod]
    public void DescSchemaMethodColumn_WithSemicolon_ShouldWork()
    {
        var query = "desc #A.entities() column Array;";

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

        Assert.AreEqual(1, table.Count, "Should work with semicolon");
    }

    [TestMethod]
    public void DescSchemaMethodColumn_HashOptional_ShouldWork()
    {
        var query = "desc A.entities() column Array";

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

        Assert.AreEqual(1, table.Count, "Should work without hash prefix");
    }

    [TestMethod]
    public void DescSchemaMethodColumn_ColumnIndex_ShouldBeCorrect()
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

        Assert.AreEqual(1, table.Count, "Should return column info");

        var columnIndex = (int)table[0][1];
        Assert.IsGreaterThanOrEqualTo(0, columnIndex, "Column index should be non-negative");
    }

    [TestMethod]
    public void DescSchemaMethodColumn_DictionaryType_ShouldWork()
    {
        var query = "desc #A.entities() column Dictionary";

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

        Assert.IsGreaterThan(0, table.Count, "Dictionary implements IEnumerable and should work");

        var columnName = (string)table[0][0];
        Assert.AreEqual("Dictionary", columnName, "Should return Dictionary column");
    }

    [TestMethod]
    public void DescSchemaMethodColumn_NestedPath_TwoLevels_ShouldWork()
    {
        var query = "desc #A.entities() column Self.Children";

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
        Assert.AreEqual(0, table.Columns.ElementAt(0).ColumnIndex);
        Assert.AreEqual("Index", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual(1, table.Columns.ElementAt(1).ColumnIndex);
        Assert.AreEqual("Type", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual(2, table.Columns.ElementAt(2).ColumnIndex);


        Assert.IsGreaterThan(0, table.Count, "Should return nested property info");
        var columnNames = table.Select(row => (string)row[0]).ToList();
        Assert.Contains("Children", columnNames, "Should contain base column name");


        Assert.IsFalse(columnNames.Any(c => c.StartsWith("Self.")),
            "Property names should be relative to Self.Children, not include Self prefix");


        var childrenRow = table.FirstOrDefault(row => (string)row[0] == "Children");
        Assert.IsNotNull(childrenRow, "Should have Children row");
        var childrenType = (string)childrenRow[2];
        Assert.Contains("BasicEntity",
            childrenType, $"Children should be BasicEntity type, got: {childrenType}");
    }

    [TestMethod]
    public void DescSchemaMethodColumn_NestedPath_ThreeLevels_ShouldWork()
    {
        var query = "desc #A.entities() column Self.Other.Children";

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


        Assert.IsGreaterThan(0, table.Count, "Should support three-level nested paths");
        var columnNames = table.Select(row => (string)row[0]).ToList();
        Assert.Contains("Children", columnNames, "Should contain Children base name");


        Assert.IsFalse(columnNames.Any(c => c.StartsWith("Self.")),
            "Property names should not include Self prefix");
        Assert.IsFalse(columnNames.Any(c => c.StartsWith("Other.")),
            "Property names should not include Other prefix");


        foreach (var row in table)
        {
            Assert.IsNotNull(row[0], "Name should not be null");
            Assert.IsInstanceOfType(row[1], typeof(int), "Index should be int");
            Assert.IsNotNull(row[2], "Type should not be null");
        }
    }

    #endregion
}
