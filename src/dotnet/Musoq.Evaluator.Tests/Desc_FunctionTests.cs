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
        for (var j = 0; j < tableFunctionsOnly.Columns.Count(); j++)
            Assert.AreEqual(tableFunctionsOnly[i][j], tableFunctionsWithMethod[i][j],
                $"Row {i}, Col {j}: desc functions #A.entities() should produce identical output to desc functions #A");
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
}
