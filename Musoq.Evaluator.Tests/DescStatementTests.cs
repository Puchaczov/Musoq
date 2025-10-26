using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema.Helpers;
using Musoq.Schema.Reflection;

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

        foreach (var row in table)
        {
            var index = (int)row[1];
            Assert.IsGreaterThanOrEqualTo(0, index, "Column index should be non-negative");
        }
        
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
    
    [TestMethod]
    public void DescSchema_DynamicSchema_ShouldReturnAvailableMethods()
    {
        var query = "desc #dynamic";

        var schema = new Dictionary<string, Type>
        {
            { "Id", typeof(int) },
            { "Name", typeof(string) },
            { "Value", typeof(decimal) }
        };

        var values = new List<dynamic>
        {
            CreateDynamicObject(1, "First", 10.5m),
            CreateDynamicObject(2, "Second", 20.5m)
        };

        var schemaProvider = new Schema.Dynamic.AnySchemaNameProvider(new Dictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)>
        {
            { "dynamic", (schema, values) }
        }, _ => [
            new SchemaMethodInfo("method1", ConstructorInfo.Empty()),
            new SchemaMethodInfo("method2", ConstructorInfo.Empty())
        ]);

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver);
        var table = vm.Run();

        Assert.AreEqual(1, table.Columns.Count(), "Should have exactly 1 column");
        Assert.AreEqual(2, table.Count, "Should return exactly 2 methods");
        
        var methodNames = table.Select(row => (string)row[0]).ToList();
        
        Assert.AreEqual("method1", methodNames[0], "Should contain 'method1' as available method");
        Assert.AreEqual("method2", methodNames[1], "Should contain 'method2' as available method");
    }
    
    [TestMethod]
    public void DescSchemaMethodWithoutParentheses_DynamicSchema_ShouldReturnColumns()
    {
        var query = "desc #dynamic.method";

        var schema = new Dictionary<string, Type>();
        var values = new List<dynamic>();

        var constructors = TypeHelper.GetConstructorsFor<TypeWithConstructor>();
        
        Assert.HasCount(1, constructors, "TypeWithConstructor should have exactly one constructor");

        var schemaProvider = new Schema.Dynamic.AnySchemaNameProvider(new Dictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)>
        {
            { "dynamic", (schema, values) }
        }, _ => [
            new SchemaMethodInfo("method", ConstructorInfo.Empty()),
            new SchemaMethodInfo("method", constructors[0])
        ]);

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver);
        var table = vm.Run();
        
        Assert.AreEqual(4, table.Columns.Count(), "Should have exactly 4 column");
        Assert.AreEqual(2, table.Count, "Should return exactly 2 method overloads");
        
        Assert.AreEqual("method", table[0][0], "Should contain 'method' as first overload");
        Assert.IsNull(table[0][1], "Should contain 'method' as second overload");
        Assert.IsNull(table[0][2], "Should contain 'method' as third overload");
        Assert.IsNull(table[0][3], "Should contain 'method' as fourth overload");
        
        Assert.AreEqual("method", table[1][0], "Should contain 'method()' as first overload");
        Assert.AreEqual("Id: System.Int32", table[1][1], "Should pass 'System.Int32: Id' as first parameter of second overload");
        Assert.AreEqual("Name: System.String", table[1][2], "Should pass 'System.String: Name' as second parameter of second overload");
        Assert.AreEqual("Value: System.Decimal", table[1][3], "Should pass 'System.Decimal: Value' as third parameter of second overload");
    }
    
    [TestMethod]
    public void DescSchemaMethodWithParentheses_DynamicSchema_ShouldReturnColumns()
    {
        var query = "desc #dynamic.method(0, 'test', 10.5d)";

        var schema = new Dictionary<string, Type>
        {
            {"Column1", typeof(double)},
            {"Column2",  typeof(string)}
        };
        var values = new List<dynamic>();

        var constructors = TypeHelper.GetConstructorsFor<TypeWithConstructor>();
        
        Assert.HasCount(1, constructors, "TypeWithConstructor should have exactly one constructor");

        var schemaProvider = new Schema.Dynamic.AnySchemaNameProvider(new Dictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)>
        {
            { "dynamic", (schema, values) }
        }, _ => [
            new SchemaMethodInfo("method", constructors[0])
        ]);

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver);
        var table = vm.Run();

        Assert.AreEqual(3, table.Columns.Count(), "Should have 3 columns: Name, Index, Type");
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Index", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual("Type", table.Columns.ElementAt(2).ColumnName);
        
        Assert.IsGreaterThanOrEqualTo(2, table.Count, "Should return at least 2 column descriptions");
        
        Assert.AreEqual("Column1", table[0][0], "Should contain 'Id' column");
        Assert.AreEqual(0, table[0][1], "Id column should have index 0");
        Assert.AreEqual("System.Double", table[0][2], "Id column should be of type System.Int32");
        
        Assert.AreEqual("Column2", table[1][0], "Should contain 'Name' column");
        Assert.AreEqual(1, table[1][1], "Name column should have index 1");
        Assert.AreEqual("System.String", table[1][2], "Name column should be of type System.String");
    }
    
    private static dynamic CreateDynamicObject(int id, string name, decimal value)
    {
        dynamic obj = new System.Dynamic.ExpandoObject();
        obj.Id = id;
        obj.Name = name;
        obj.Value = value;
        return obj;
    }
    
    private record TypeWithConstructor(int Id, string Name, decimal Value);
}

