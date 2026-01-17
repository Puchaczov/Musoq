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
        var table = vm.Run(TestContext.CancellationToken);

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
        var table = vm.Run(TestContext.CancellationToken);

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
        var table = vm.Run(TestContext.CancellationToken);

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
        var table = vm.Run(TestContext.CancellationToken);

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
        var table = vm.Run(TestContext.CancellationToken);

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
        var table = vm.Run(TestContext.CancellationToken);

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
        var table = vm.Run(TestContext.CancellationToken);

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
        var table = vm.Run(TestContext.CancellationToken);

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
        var table = vm.Run(TestContext.CancellationToken);

        
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
        var table = vm.Run(TestContext.CancellationToken);

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
        var table = vm.Run(TestContext.CancellationToken);

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
        var table = vm.Run(TestContext.CancellationToken);

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
        var table = vm.Run(TestContext.CancellationToken);

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
        var table = vm.Run(TestContext.CancellationToken);

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
        var table = vm.Run(TestContext.CancellationToken);

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
        var table = vm.Run(TestContext.CancellationToken);

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
        var table = vm.Run(TestContext.CancellationToken);
        
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
        var table = vm.Run(TestContext.CancellationToken);

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
    
    [TestMethod]
    public void DescFunctionsSchema_ShouldReturnMethodsWithDescriptions()
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

        Assert.AreEqual(2, table.Columns.Count(), "Should have 2 columns: Method and Description");
        Assert.AreEqual("Method", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Description", table.Columns.ElementAt(1).ColumnName);
        
        Assert.IsGreaterThan(0, table.Count, "Should return at least one method");
        
        var methodSignatures = table.Select(row => (string)row[0]).ToList();
        
        Assert.IsTrue(methodSignatures.Any(m => m.Contains("Trim(")), "Should contain library methods like 'Trim'");
        Assert.IsTrue(methodSignatures.Any(m => m.Contains("Substring(")), "Should contain library methods like 'Substring'");
        
        foreach (var signature in methodSignatures)
        {
            Assert.IsTrue(signature.Contains("(") && signature.Contains(")"), 
                $"Method signature should include parentheses: {signature}");
            Assert.Contains(" ",
signature, $"Method signature should include return type and method name: {signature}");
        }
        
        foreach (var row in table)
        {
            var description = (string)row[1];
            Assert.IsNotNull(description, "Description should not be null");
        }
    }

    [TestMethod]
    public void DescFunctionsSchema_ShouldShowMethodSignatures()
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

        var trimRow = table.FirstOrDefault(row => ((string)row[0]).Contains("Trim("));
        Assert.IsNotNull(trimRow, "Should have Trim method from library");
        
        var methodSignature = (string)trimRow[0];
        Assert.Contains("Trim(", methodSignature, "Method should contain method signature");
        Assert.Contains(" ", methodSignature, "Method signature should include return type and method name");
    }

    [TestMethod]
    public void DescFunctionsSchema_WithSemicolon_ShouldWork()
    {
        var query = "desc functions #A;";

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

        Assert.AreEqual(2, table.Columns.Count(), "Should have 2 columns even with semicolon");
        Assert.IsGreaterThan(0, table.Count, "Should return methods even with semicolon");
    }

    [TestMethod]
    public void DescFunctionsSchema_CaseInsensitive_ShouldWork()
    {
        var query = "DESC FUNCTIONS #A";

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

        Assert.IsGreaterThan(0, table.Count, "DESC FUNCTIONS should be case insensitive");
    }

    [TestMethod]
    public void DescFunctionsSchema_EmptySource_ShouldStillReturnMethods()
    {
        var query = "desc functions #A";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", []
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsGreaterThan(0, table.Count, "Should return library methods even with empty source");
        Assert.AreEqual(2, table.Columns.Count(), "Should have Method and Description columns");
    }

    [TestMethod]
    public void DescFunctionsSchema_MultipleSchemas_OnlyDescribesSpecified()
    {
        var query = "desc functions #A";

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
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsGreaterThan(0, table.Count, "Should return methods");
        Assert.AreEqual(2, table.Columns.Count(), "Should have 2 columns");
    }

    [TestMethod]
    public void DescFunctionsSchema_ResultColumns_HaveCorrectTypes()
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

        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType, "Method column should be string");
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType, "Description column should be string");
    }

    [TestMethod]
    public void DescFunctionsSchema_ShouldNotShowInternalMethods()
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

        var methodSignatures = table.Select(row => (string)row[0]).ToList();
        
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.InternalApplyAddOperator))), $"Should not show {nameof(Plugins.LibraryBase.InternalApplyAddOperator)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.InternalApplySubtractOperator))), $"Should not show {nameof(Plugins.LibraryBase.InternalApplySubtractOperator)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.InternalApplyMultiplyOperator))), $"Should not show {nameof(Plugins.LibraryBase.InternalApplyMultiplyOperator)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.InternalApplyDivideOperator))), $"Should not show {nameof(Plugins.LibraryBase.InternalApplyDivideOperator)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.InternalApplyModuloOperator))), $"Should not show {nameof(Plugins.LibraryBase.InternalApplyModuloOperator)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.InternalGreaterThanOperator))), $"Should not show {nameof(Plugins.LibraryBase.InternalGreaterThanOperator)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.InternalLessThanOperator))), $"Should not show {nameof(Plugins.LibraryBase.InternalLessThanOperator)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.InternalEqualOperator))), $"Should not show {nameof(Plugins.LibraryBase.InternalEqualOperator)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.InternalNotEqualOperator))), $"Should not show {nameof(Plugins.LibraryBase.InternalNotEqualOperator)}");
        
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.TryConvertToInt32Strict))), $"Should not show {nameof(Plugins.LibraryBase.TryConvertToInt32Strict)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.TryConvertToInt64Strict))), $"Should not show {nameof(Plugins.LibraryBase.TryConvertToInt64Strict)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.TryConvertToDecimalStrict))), $"Should not show {nameof(Plugins.LibraryBase.TryConvertToDecimalStrict)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.TryConvertToInt32Comparison))), $"Should not show {nameof(Plugins.LibraryBase.TryConvertToInt32Comparison)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.TryConvertToInt64Comparison))), $"Should not show {nameof(Plugins.LibraryBase.TryConvertToInt64Comparison)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.TryConvertToDecimalComparison))), $"Should not show {nameof(Plugins.LibraryBase.TryConvertToDecimalComparison)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.TryConvertNumericOnly))), $"Should not show {nameof(Plugins.LibraryBase.TryConvertNumericOnly)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.TryConvertToInt32NumericOnly))), $"Should not show {nameof(Plugins.LibraryBase.TryConvertToInt32NumericOnly)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.TryConvertToInt64NumericOnly))), $"Should not show {nameof(Plugins.LibraryBase.TryConvertToInt64NumericOnly)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.TryConvertToDecimalNumericOnly))), $"Should not show {nameof(Plugins.LibraryBase.TryConvertToDecimalNumericOnly)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.TryConvertToDoubleNumericOnly))), $"Should not show {nameof(Plugins.LibraryBase.TryConvertToDoubleNumericOnly)}");
        
        Assert.IsTrue(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.Trim) + "(")), $"Should still show public methods like {nameof(Plugins.LibraryBase.Trim)}");
        Assert.IsTrue(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.Substring) + "(")), $"Should still show public methods like {nameof(Plugins.LibraryBase.Substring)}");
        
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.SetAggregateValues))), $"Should not show aggregation set methods like {nameof(Plugins.LibraryBase.SetAggregateValues)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.SetMax))), $"Should not show aggregation set methods like {nameof(Plugins.LibraryBase.SetMax)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.SetMin))), $"Should not show aggregation set methods like {nameof(Plugins.LibraryBase.SetMin)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.SetCount))), $"Should not show aggregation set methods like {nameof(Plugins.LibraryBase.SetCount)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(Plugins.LibraryBase.SetAvg))), $"Should not show aggregation set methods like {nameof(Plugins.LibraryBase.SetAvg)}");
        
        var maxMethod = methodSignatures.FirstOrDefault(m => m.Contains(nameof(Plugins.LibraryBase.Max) + "("));
        if (maxMethod != null)
        {
            Assert.DoesNotContain("Group group", maxMethod, $"Should not show injected Group parameter: {maxMethod}");
        }
    }

    [TestMethod]
    public void DescFunctionsSchema_DynamicSchema_ShouldReturnMethods()
    {
        var query = "desc functions #dynamic";

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
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count(), "Should have exactly 2 columns: Method and Description");
        Assert.IsGreaterThan(0, table.Count, "Should return library methods");
        
        var methodSignatures = table.Select(row => (string)row[0]).ToList();
        
        
        Assert.IsTrue(methodSignatures.Any(m => m.Contains("Trim(")), "Should contain library methods");
        
        
        foreach (var row in table)
        {
            var description = (string)row[1];
            Assert.IsNotNull(description, "Description should not be null");
        }
    }
    
    [TestMethod]
    public void DescFunctionsSchemaMethod_ShouldReturnMethodsWithDescriptions()
    {
        var query = "desc functions #A.entities";

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

        Assert.AreEqual(2, table.Columns.Count(), "Should have 2 columns: Method and Description");
        Assert.AreEqual("Method", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Description", table.Columns.ElementAt(1).ColumnName);
        
        Assert.IsGreaterThan(0, table.Count, "Should return at least one library method");
        
        var methodSignatures = table.Select(row => (string)row[0]).ToList();
        Assert.IsTrue(methodSignatures.Any(m => m.Contains("Trim(") || m.Contains("Substring(")), 
            "Should contain library methods");
    }

    [TestMethod]
    public void DescFunctionsSchemaMethodWithParentheses_ShouldReturnMethodsWithDescriptions()
    {
        var query = "desc functions #A.entities()";

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

        Assert.AreEqual(2, table.Columns.Count(), "Should have 2 columns: Method and Description");
        Assert.AreEqual("Method", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Description", table.Columns.ElementAt(1).ColumnName);
        
        Assert.IsGreaterThan(0, table.Count, "Should return at least one method");
        
        var methodSignatures = table.Select(row => (string)row[0]).ToList();
        Assert.IsTrue(methodSignatures.Any(m => m.Contains("Trim(") || m.Contains("Substring(")), 
            "Should contain library methods");
    }

    [TestMethod]
    public void DescFunctionsSchemaMethodWithArguments_ShouldReturnMethodsWithDescriptions()
    {
        var query = "desc functions #A.entities('filter')";

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

        Assert.AreEqual(2, table.Columns.Count(), "Should have 2 columns: Method and Description");
        Assert.IsGreaterThan(0, table.Count, "Should return at least one method");
        
        var methodSignatures = table.Select(row => (string)row[0]).ToList();
        Assert.IsTrue(methodSignatures.Any(m => m.Contains("Trim(") || m.Contains("Substring(")), 
            "Should contain library methods");
    }
    
    [TestMethod]
    public void DescFunctionsSchema_ShouldFormatNullableTypesCorrectly()
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

        var methodSignatures = table.Select(row => (string)row[0]).ToList();
        
        foreach (var signature in methodSignatures)
        {
            Assert.DoesNotContain("Nullable`1",
signature, $"Signature should not contain 'Nullable`1', but got: {signature}");
            Assert.DoesNotContain("Nullable<",
signature, $"Signature should use '?' instead of 'Nullable<', but got: {signature}");
        }
        
        var nullableSignatures = methodSignatures.Where(m => m.Contains("?")).ToList();
        Assert.IsGreaterThan(0, nullableSignatures.Count, "Should have methods with nullable parameters");
        
        var sampleNullableMethod = nullableSignatures.FirstOrDefault(m => m.Contains("int?") || m.Contains("decimal?"));
        Assert.IsNotNull(sampleNullableMethod, 
            "Should have at least one method with properly formatted nullable type (e.g., 'int?' or 'decimal?')");
    }
    
    [TestMethod]
    public void DescFunctionsSchema_ShouldFormatGenericTypesCorrectly()
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

        var methodSignatures = table.Select(row => (string)row[0]).ToList();
        
        foreach (var signature in methodSignatures)
        {
            Assert.IsFalse(signature.Contains("`1") || signature.Contains("`2") || signature.Contains("`3"), 
                $"Signature should not contain backtick notation like `1, but got: {signature}");
        }
        
        var genericSignatures = methodSignatures.Where(m => m.Contains("<") && m.Contains(">")).ToList();
        Assert.IsGreaterThan(0, genericSignatures.Count, "Should have generic methods");
        
        foreach (var signature in genericSignatures)
        {
            var genericPart = signature.Substring(signature.IndexOf('<'));
            Assert.DoesNotContain("System.",
genericPart, $"Generic parameters should not include System. prefix: {signature}");
        }
    }
    
    [TestMethod]
    public void DescFunctionsSchema_ShouldUseCSharpTypeAliases()
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

        var methodSignatures = table.Select(row => (string)row[0]).ToList();
        
        Assert.IsTrue(methodSignatures.Any(m => m.Contains("string ")), 
            "Should use 'string' instead of 'String'");
        
        foreach (var signature in methodSignatures)
        {
            Assert.DoesNotContain("Int32 ",
signature, $"Should use 'int' instead of 'Int32': {signature}");
            Assert.DoesNotContain("Boolean ",
signature, $"Should use 'bool' instead of 'Boolean': {signature}");
            Assert.DoesNotContain("String ",
signature, $"Should use 'string' instead of 'String': {signature}");
            Assert.DoesNotContain("Decimal ",
signature, $"Should use 'decimal' instead of 'Decimal': {signature}");
        }
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

    public TestContext TestContext { get; set; }
}

