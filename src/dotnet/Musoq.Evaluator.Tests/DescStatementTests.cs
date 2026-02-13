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

[TestClass]
public class DescStatementTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

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

        var schemaProvider = new AnySchemaNameProvider(
            new Dictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)>
            {
                { "dynamic", (schema, values) }
            }, _ =>
            [
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

        var schemaProvider = new AnySchemaNameProvider(
            new Dictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)>
            {
                { "dynamic", (schema, values) }
            }, _ =>
            [
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
        Assert.AreEqual("Id: System.Int32", table[1][1],
            "Should pass 'System.Int32: Id' as first parameter of second overload");
        Assert.AreEqual("Name: System.String", table[1][2],
            "Should pass 'System.String: Name' as second parameter of second overload");
        Assert.AreEqual("Value: System.Decimal", table[1][3],
            "Should pass 'System.Decimal: Value' as third parameter of second overload");
    }

    [TestMethod]
    public void DescSchemaMethodWithParentheses_DynamicSchema_ShouldReturnColumns()
    {
        var query = "desc #dynamic.method(0, 'test', 10.5d)";

        var schema = new Dictionary<string, Type>
        {
            { "Column1", typeof(double) },
            { "Column2", typeof(string) }
        };
        var values = new List<dynamic>();

        var constructors = TypeHelper.GetConstructorsFor<TypeWithConstructor>();

        Assert.HasCount(1, constructors, "TypeWithConstructor should have exactly one constructor");

        var schemaProvider = new AnySchemaNameProvider(
            new Dictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)>
            {
                { "dynamic", (schema, values) }
            }, _ =>
            [
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

        Assert.AreEqual(4, table.Columns.Count(), "Should have 4 columns: Method, Description, Category, and Source");
        Assert.AreEqual("Method", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Description", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual("Category", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual("Source", table.Columns.ElementAt(3).ColumnName);

        Assert.IsGreaterThan(0, table.Count, "Should return at least one method");

        var methodSignatures = table.Select(row => (string)row[0]).ToList();

        Assert.IsTrue(methodSignatures.Any(m => m.Contains("Trim(")), "Should contain library methods like 'Trim'");
        Assert.IsTrue(methodSignatures.Any(m => m.Contains("Substring(")),
            "Should contain library methods like 'Substring'");

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
            var category = (string)row[2];
            var source = (string)row[3];
            Assert.IsNotNull(description, "Description should not be null");
            Assert.IsNotNull(category, "Category should not be null");
            Assert.IsNotNull(source, "Source should not be null");
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

        Assert.AreEqual(4, table.Columns.Count(), "Should have 4 columns even with semicolon");
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
        Assert.AreEqual(4, table.Columns.Count(), "Should have Method, Description, Category, and Source columns");
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
        Assert.AreEqual(4, table.Columns.Count(), "Should have 4 columns");
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
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType, "Category column should be string");
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(3).ColumnType, "Source column should be string");
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

        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.InternalApplyAddOperator))),
            $"Should not show {nameof(LibraryBase.InternalApplyAddOperator)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.InternalApplySubtractOperator))),
            $"Should not show {nameof(LibraryBase.InternalApplySubtractOperator)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.InternalApplyMultiplyOperator))),
            $"Should not show {nameof(LibraryBase.InternalApplyMultiplyOperator)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.InternalApplyDivideOperator))),
            $"Should not show {nameof(LibraryBase.InternalApplyDivideOperator)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.InternalApplyModuloOperator))),
            $"Should not show {nameof(LibraryBase.InternalApplyModuloOperator)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.InternalGreaterThanOperator))),
            $"Should not show {nameof(LibraryBase.InternalGreaterThanOperator)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.InternalLessThanOperator))),
            $"Should not show {nameof(LibraryBase.InternalLessThanOperator)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.InternalEqualOperator))),
            $"Should not show {nameof(LibraryBase.InternalEqualOperator)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.InternalNotEqualOperator))),
            $"Should not show {nameof(LibraryBase.InternalNotEqualOperator)}");

        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.TryConvertToInt32Strict))),
            $"Should not show {nameof(LibraryBase.TryConvertToInt32Strict)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.TryConvertToInt64Strict))),
            $"Should not show {nameof(LibraryBase.TryConvertToInt64Strict)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.TryConvertToDecimalStrict))),
            $"Should not show {nameof(LibraryBase.TryConvertToDecimalStrict)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.TryConvertToInt32Comparison))),
            $"Should not show {nameof(LibraryBase.TryConvertToInt32Comparison)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.TryConvertToInt64Comparison))),
            $"Should not show {nameof(LibraryBase.TryConvertToInt64Comparison)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.TryConvertToDecimalComparison))),
            $"Should not show {nameof(LibraryBase.TryConvertToDecimalComparison)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.TryConvertNumericOnly))),
            $"Should not show {nameof(LibraryBase.TryConvertNumericOnly)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.TryConvertToInt32NumericOnly))),
            $"Should not show {nameof(LibraryBase.TryConvertToInt32NumericOnly)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.TryConvertToInt64NumericOnly))),
            $"Should not show {nameof(LibraryBase.TryConvertToInt64NumericOnly)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.TryConvertToDecimalNumericOnly))),
            $"Should not show {nameof(LibraryBase.TryConvertToDecimalNumericOnly)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.TryConvertToDoubleNumericOnly))),
            $"Should not show {nameof(LibraryBase.TryConvertToDoubleNumericOnly)}");

        Assert.IsTrue(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.Trim) + "(")),
            $"Should still show public methods like {nameof(LibraryBase.Trim)}");
        Assert.IsTrue(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.Substring) + "(")),
            $"Should still show public methods like {nameof(LibraryBase.Substring)}");

        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.SetAggregateValues))),
            $"Should not show aggregation set methods like {nameof(LibraryBase.SetAggregateValues)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.SetMax))),
            $"Should not show aggregation set methods like {nameof(LibraryBase.SetMax)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.SetMin))),
            $"Should not show aggregation set methods like {nameof(LibraryBase.SetMin)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.SetCount))),
            $"Should not show aggregation set methods like {nameof(LibraryBase.SetCount)}");
        Assert.IsFalse(methodSignatures.Any(m => m.Contains(nameof(LibraryBase.SetAvg))),
            $"Should not show aggregation set methods like {nameof(LibraryBase.SetAvg)}");

        var maxMethod = methodSignatures.FirstOrDefault(m => m.Contains(nameof(LibraryBase.Max) + "("));
        if (maxMethod != null)
            Assert.DoesNotContain("Group group", maxMethod, $"Should not show injected Group parameter: {maxMethod}");
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

        var schemaProvider = new AnySchemaNameProvider(
            new Dictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)>
            {
                { "dynamic", (schema, values) }
            }, _ =>
            [
                new SchemaMethodInfo("method1", ConstructorInfo.Empty()),
                new SchemaMethodInfo("method2", ConstructorInfo.Empty())
            ]);

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Columns.Count(),
            "Should have exactly 4 columns: Method, Description, Category, and Source");
        Assert.IsGreaterThan(0, table.Count, "Should return library methods");

        var methodSignatures = table.Select(row => (string)row[0]).ToList();


        Assert.IsTrue(methodSignatures.Any(m => m.Contains("Trim(")), "Should contain library methods");

        foreach (var row in table)
        {
            var description = (string)row[1];
            var category = (string)row[2];
            var source = (string)row[3];
            Assert.IsNotNull(description, "Description should not be null");
            Assert.IsNotNull(category, "Category should not be null");
            Assert.IsNotNull(source, "Source should not be null");
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

        Assert.AreEqual(4, table.Columns.Count(), "Should have 4 columns: Method, Description, Category, and Source");
        Assert.AreEqual("Method", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Description", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual("Category", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual("Source", table.Columns.ElementAt(3).ColumnName);

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

        Assert.AreEqual(4, table.Columns.Count(), "Should have 4 columns: Method, Description, Category, and Source");
        Assert.AreEqual("Method", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Description", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual("Category", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual("Source", table.Columns.ElementAt(3).ColumnName);

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

        Assert.AreEqual(4, table.Columns.Count(), "Should have 4 columns: Method, Description, Category, and Source");
        Assert.IsGreaterThan(0, table.Count, "Should return at least one method");

        var methodSignatures = table.Select(row => (string)row[0]).ToList();
        Assert.IsTrue(methodSignatures.Any(m => m.Contains("Trim(") || m.Contains("Substring(")),
            "Should contain library methods");
    }

    [TestMethod]
    public void DescFunctionsSchemaMethod_ShouldReturnSameResultAsDescFunctionsSchema()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vmFunctionsOnly = CreateAndRunVirtualMachine("desc functions #A", sources);
        var tableFunctionsOnly = vmFunctionsOnly.Run(TestContext.CancellationToken);

        var vmFunctionsWithMethod = CreateAndRunVirtualMachine("desc functions #A.entities()", sources);
        var tableFunctionsWithMethod = vmFunctionsWithMethod.Run(TestContext.CancellationToken);

        Assert.AreEqual(tableFunctionsOnly.Columns.Count(), tableFunctionsWithMethod.Columns.Count(),
            "desc functions #A.entities() should have same column count as desc functions #A");
        Assert.AreEqual(tableFunctionsOnly.Count, tableFunctionsWithMethod.Count,
            "desc functions #A.entities() should have same row count as desc functions #A");

        for (var i = 0; i < tableFunctionsOnly.Count; i++)
        {
            for (var j = 0; j < tableFunctionsOnly.Columns.Count(); j++)
            {
                Assert.AreEqual(tableFunctionsOnly[i][j], tableFunctionsWithMethod[i][j],
                    $"Row {i}, Col {j}: desc functions #A.entities() should produce identical output to desc functions #A");
            }
        }
    }

    [TestMethod]
    public void DescFunctionsSchemaMethodWithArgs_ShouldReturnSameResultAsDescFunctionsSchema()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vmFunctionsOnly = CreateAndRunVirtualMachine("desc functions #A", sources);
        var tableFunctionsOnly = vmFunctionsOnly.Run(TestContext.CancellationToken);

        var vmFunctionsWithMethod = CreateAndRunVirtualMachine("desc functions #A.entities('filter')", sources);
        var tableFunctionsWithMethod = vmFunctionsWithMethod.Run(TestContext.CancellationToken);

        Assert.AreEqual(tableFunctionsOnly.Columns.Count(), tableFunctionsWithMethod.Columns.Count(),
            "desc functions #A.entities('filter') should have same column count as desc functions #A");
        Assert.AreEqual(tableFunctionsOnly.Count, tableFunctionsWithMethod.Count,
            "desc functions #A.entities('filter') should have same row count as desc functions #A");
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
            Assert.IsFalse(signature.Contains("`1") || signature.Contains("`2") || signature.Contains("`3"),
                $"Signature should not contain backtick notation like `1, but got: {signature}");

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
        dynamic obj = new ExpandoObject();
        obj.Id = id;
        obj.Name = name;
        obj.Value = value;
        return obj;
    }

    private record TypeWithConstructor(int Id, string Name, decimal Value);

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
