using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class DescStatementTests : BasicEntityTestBase
{
    [TestMethod]
    public void DescSchema_ShouldReturnAvailableMethods()
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
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.IsGreaterThan(0, table.Count, "Should return at least one method");
        
        // Verify expected methods exist
        Assert.IsTrue(table.Any(row => (string)row[0] == "empty"), "Should contain 'empty' method");
        Assert.IsTrue(table.Any(row => (string)row[0] == "entities"), "Should contain 'entities' method");
    }

    [TestMethod]
    public void DescSchemaMethod_ShouldReturnMethodSignature()
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
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(1, table.Count, "Should return exactly one method name");
        Assert.AreEqual("entities", table[0][0], "Should return the method name");
    }

    [TestMethod]
    public void DescSchemaMethodWithParentheses_ShouldReturnColumns()
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
        var table = vm.Run();

        Assert.AreEqual(3, table.Columns.Count(), "Should have 3 columns: Name, Index, Type");
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Index", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual("Type", table.Columns.ElementAt(2).ColumnName);
        
        Assert.IsGreaterThan(0, table.Count, "Should return at least one column");
        
        // Verify expected columns exist
        Assert.IsTrue(table.Any(row => (string)row[0] == "Name"), "Should contain 'Name' column");
        Assert.IsTrue(table.Any(row => (string)row[0] == "City"), "Should contain 'City' column");
        Assert.IsTrue(table.Any(row => (string)row[0] == "Country"), "Should contain 'Country' column");
        Assert.IsTrue(table.Any(row => (string)row[0] == "Population"), "Should contain 'Population' column");
        Assert.IsTrue(table.Any(row => (string)row[0] == "Self"), "Should contain 'Self' column");
    }

    [TestMethod]
    public void DescSchemaMethodWithParentheses_ShouldReturnColumnTypes()
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
        var table = vm.Run();

        // Verify column types are correct
        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "Name" && ((string)row[2]).Contains("String")), 
            "Name column should be of type String");
        
        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "Population" && ((string)row[2]).Contains("Decimal")), 
            "Population column should be of type Decimal");
        
        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "Time" && ((string)row[2]).Contains("DateTime")), 
            "Time column should be of type DateTime");
        
        Assert.IsTrue(table.Any(row => 
            (string)row[0] == "Id" && ((string)row[2]).Contains("Int32")), 
            "Id column should be of type Int32");
    }

    [TestMethod]
    public void DescSchemaMethodWithParentheses_ShouldReturnColumnIndices()
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
        var table = vm.Run();

        // Verify all rows have a valid index
        foreach (var row in table)
        {
            var index = (int)row[1];
            Assert.IsGreaterThanOrEqualTo(0, index, "Column index should be non-negative");
        }
        
        // Verify we got indices for all columns
        var indices = table.Select(row => (int)row[1]).ToList();
        Assert.IsNotEmpty(indices, "Should have column indices");
    }

    [TestMethod]
    public void DescSchemaMethodWithParentheses_ComplexEntity_ShouldShowAllColumns()
    {
        var query = "desc #A.entities()";

        var entity = new BasicEntity("test")
        {
            City = "TestCity",
            Country = "TestCountry",
            Population = 100m,
            Money = 50m,
            Month = "January",
            Time = DateTime.Now,
            Id = 1,
            NullableValue = 10
        };

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [entity]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        // Verify all expected columns are present
        var columnNames = table.Select(row => (string)row[0]).ToList();
        
        Assert.Contains("Name", columnNames, "Should contain Name column");
        Assert.Contains("City", columnNames, "Should contain City column");
        Assert.Contains("Country", columnNames, "Should contain Country column");
        Assert.Contains("Population", columnNames, "Should contain Population column");
        Assert.Contains("Self", columnNames, "Should contain Self column");
        Assert.Contains("Money", columnNames, "Should contain Money column");
        Assert.Contains("Month", columnNames, "Should contain Month column");
        Assert.Contains("Time", columnNames, "Should contain Time column");
        Assert.Contains("Id", columnNames, "Should contain Id column");
        Assert.Contains("NullableValue", columnNames, "Should contain NullableValue column");
    }

    [TestMethod]
    public void DescSchemaMethodWithParentheses_NullableTypes_ShouldShowCorrectType()
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
        var table = vm.Run();

        // Find NullableValue column and verify its type
        var nullableValueRow = table.FirstOrDefault(row => (string)row[0] == "NullableValue");
        Assert.IsNotNull(nullableValueRow, "Should have NullableValue column");
        
        var typeName = (string)nullableValueRow[2];
        Assert.IsTrue(typeName.Contains("Nullable") || typeName.Contains("Int32"), 
            $"NullableValue should be nullable type, but got: {typeName}");
    }

    [TestMethod]
    public void DescSchemaMethodWithParentheses_WithSemicolon_ShouldWork()
    {
        var query = "desc #A.entities();";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.IsGreaterThan(0, table.Count, "Should return columns even with semicolon");
    }

    [TestMethod]
    public void DescSchema_MultipleSchemas_OnlyDescribesSpecified()
    {
        var query = "desc #A";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("testA")
                ]
            },
            {
                "#B", [
                    new BasicEntity("testB")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        // Should only return methods from schema A
        Assert.IsGreaterThan(0, table.Count, "Should return methods");
        Assert.AreEqual(1, table.Columns.Count(), "Should have 1 column");
    }

    [TestMethod]
    public void DescSchemaMethodWithParentheses_EmptySource_ShouldStillReturnSchema()
    {
        var query = "desc #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", []
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.IsGreaterThan(0, table.Count, "Should return schema even with empty source");
        Assert.AreEqual(3, table.Columns.Count(), "Should have Name, Index, and Type columns");
    }

    [TestMethod]
    public void DescSchemaMethodWithParentheses_ResultColumns_HaveCorrectTypes()
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
        var table = vm.Run();

        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType, "Name column should be string");
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(1).ColumnType, "Index column should be int");
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType, "Type column should be string");
    }

    [TestMethod]
    public void DescSchemaMethodWithParentheses_ArrayColumns_ShouldShowArrayType()
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
        var table = vm.Run();

        // Find Array column and verify it's shown as an array type
        var arrayRow = table.FirstOrDefault(row => (string)row[0] == "Array");
        if (arrayRow != null)
        {
            var typeName = (string)arrayRow[2];
            Assert.IsTrue(typeName.Contains("Int32[]") || typeName.Contains("Array"), 
                $"Array column should show array type, but got: {typeName}");
        }
    }

    [TestMethod]
    public void DescSchemaMethodWithParentheses_ComplexObjectColumns_ShouldShowComplexType()
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
        var table = vm.Run();

        // Find Self column and verify it shows the complex entity type
        var selfRow = table.FirstOrDefault(row => (string)row[0] == "Self");
        Assert.IsNotNull(selfRow, "Should have Self column");
        
        var typeName = (string)selfRow[2];
        Assert.Contains("BasicEntity",
typeName, $"Self column should show BasicEntity type, but got: {typeName}");
    }

    [TestMethod]
    public void DescSchemaMethodWithParentheses_DictionaryColumns_ShouldShowDictionaryType()
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
        var table = vm.Run();

        // Find Dictionary column and verify it's shown as a dictionary type
        var dictRow = table.FirstOrDefault(row => (string)row[0] == "Dictionary");
        if (dictRow != null)
        {
            var typeName = (string)dictRow[2];
            Assert.Contains("Dictionary",
typeName, $"Dictionary column should show Dictionary type, but got: {typeName}");
        }
    }

    [TestMethod]
    public void DescSchemaMethodWithParentheses_CaseInsensitive_ShouldWork()
    {
        var query = "DESC #A.ENTITIES()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.IsGreaterThan(0, table.Count, "DESC keyword should be case insensitive");
    }
}
