using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Interpretation Functions (Section 7 of specification).
///     Tests Interpret, Parse, TryInterpret, TryParse, and InterpretAt functions.
/// </summary>
[TestClass]
public class InterpretationFunctionsFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Section 7: Combined Interpretation Functions

    /// <summary>
    ///     Tests using both Interpret and Parse in same query.
    /// </summary>
    [TestMethod]
    public void InterpretAndParse_Combined_ShouldWork()
    {
        var query = (
            "\n"
            + "            binary BinaryData {\n"
            + "                Value: int le\n"
            + "            };\n"
            + "            text TextData {\n"
            + "                Name: until ':'\n"
            + "            };\n"
            + "            select b.Name, d.Value, t.Name as TextName\n"
            + "            from #btest.bytes() b\n"
            + "            cross apply Interpret<BinaryData>(b.Content) d\n"
            + "            cross apply #ttest.lines() l\n"
            + "            cross apply Parse<TextData>(l.Line) t"
        );

        var binaryData = "\n\u0000\u0000\u0000"u8.ToArray();
        var binaryEntities = new[]
        {
            new BinaryEntity { Name = "test.bin", Data = binaryData },
        };
        var textEntities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "Label:data" },
        };

        var schemaProvider = new MixedSchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#btest", binaryEntities } },
            new Dictionary<string, IEnumerable<TextEntity>> { { "#ttest", textEntities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Value", typeof(int)),
            ("TextName", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["test.bin", 10, "Label"]);
    }

    #endregion

    #region Section 7: Interpretation with Aggregation

    /// <summary>
    ///     Tests interpretation results with GROUP BY.
    /// </summary>
    [TestMethod]
    public void Interpret_WithGroupBy_ShouldAggregate()
    {
        var query = (
            "\n"
            + "            binary Data {\n"
            + "                Category: byte,\n"
            + "                Value: int le\n"
            + "            };\n"
            + "            select d.Category, Sum(d.Value) as Total from #test.bytes() b\n"
            + "            cross apply Interpret<Data>(b.Content) d\n"
            + "            group by d.Category\n"
            + "            order by d.Category"
        );

        var data1 = new byte[] { 0x01, 0x0A, 0x00, 0x00, 0x00 };
        var data2 = new byte[] { 0x02, 0x14, 0x00, 0x00, 0x00 };
        var data3 = new byte[] { 0x01, 0x1E, 0x00, 0x00, 0x00 };
        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = data1 },
            new BinaryEntity { Name = "2.bin", Data = data2 },
            new BinaryEntity { Name = "3.bin", Data = data3 },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Category", typeof(byte)), ("Total", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)1, 40], [(byte)2, 20]);
    }

    #endregion

    #region Section 7.1: Interpret Function

    /// <summary>
    ///     Tests basic Interpret function for binary data.
    /// </summary>
    [TestMethod]
    public void Interpret_BasicBinary_ShouldParse()
    {
        var query = (
            "\n"
            + "            binary Header {\n"
            + "                Magic: int le,\n"
            + "                Version: byte\n"
            + "            };\n"
            + "            select h.Magic, h.Version from #test.bytes() b\n"
            + "            cross apply Interpret<Header>(b.Content) h"
        );

        var data = new byte[] { 0x41, 0x42, 0x43, 0x44, 0x01 };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Data = data },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Magic", typeof(int)), ("Version", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [0x44434241, (byte)1]);
    }

    /// <summary>
    ///     Tests Interpret with multiple schema definitions.
    /// </summary>
    [TestMethod]
    public void Interpret_MultipleSchemas_ShouldSelectCorrect()
    {
        var query = (
            "\n"
            + "            binary First {\n"
            + "                Value: int le\n"
            + "            };\n"
            + "            binary Second {\n"
            + "                A: short le,\n"
            + "                B: short le\n"
            + "            };\n"
            + "            select f.Value, s.A, s.B from #test.bytes() b\n"
            + "            cross apply Interpret<First>(b.Content) f\n"
            + "            cross apply Interpret<Second>(b.Content) s"
        );

        var data = new byte[] { 0x01, 0x00, 0x02, 0x00 };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Data = data },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x00020001, table[0].ReadValue(0));
        Assert.AreEqual((short)1, table[0].ReadValue(1));
        Assert.AreEqual((short)2, table[0].ReadValue(2));
    }

    #endregion

    #region Section 7.2: Parse Function

    /// <summary>
    ///     Tests basic Parse function for text data.
    /// </summary>
    [TestMethod]
    public void Parse_BasicText_ShouldParse()
    {
        var query = (
            "\n"
            + "            text Record {\n"
            + "                Name: until ',',\n"
            + "                Value: rest trim\n"
            + "            };\n"
            + "            select r.Name, r.Value from #test.lines() l\n"
            + "            cross apply Parse<Record>(l.Line) r"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "Key,Value123" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Key", "Value123"]);
    }

    /// <summary>
    ///     Tests Parse with multiple text schemas.
    /// </summary>
    [TestMethod]
    public void Parse_MultipleTextSchemas_ShouldWork()
    {
        var query = (
            "\n"
            + "            text CsvRow {\n"
            + "                Field1: until ',',\n"
            + "                Field2: until ',',\n"
            + "                Field3: rest\n"
            + "            };\n"
            + "            text KeyValue {\n"
            + "                Key: until ':',\n"
            + "                Value: rest\n"
            + "            };\n"
            + "            select c.Field1, c.Field2, c.Field3 from #test.lines() l\n"
            + "            cross apply Parse<CsvRow>(l.Line) c"
        );

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "A,B,C" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A", table[0].ReadValue(0));
        Assert.AreEqual("B", table[0].ReadValue(1));
        Assert.AreEqual("C", table[0].ReadValue(2));
    }

    #endregion

    #region Section 7.3: TryInterpret Function

    /// <summary>
    ///     Tests TryInterpret with valid data.
    /// </summary>
    [TestMethod]
    public void TryInterpret_ValidData_ShouldSucceed()
    {
        var query = (
            "\n"
            + "            binary Data {\n"
            + "                Value: int le\n"
            + "            };\n"
            + "            select d.Value from #test.bytes() b\n"
            + "            outer apply TryInterpret<Data>(b.Content) d\n"
            + "            where d.Value is not null"
        );

        var data = "\n\u0000\u0000\u0000"u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Data = data },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(10, table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests TryInterpret with insufficient data.
    /// </summary>
    [TestMethod]
    public void TryInterpret_InsufficientData_ShouldReturnNull()
    {
        var query = (
            "\n"
            + "            binary Data {\n"
            + "                Value: long le\n"
            + "            };\n"
            + "            select d.Value from #test.bytes() b\n"
            + "            outer apply TryInterpret<Data>(b.Content) d"
        );

        var data = new byte[] { 0x01, 0x02 };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Data = data },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(long?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { null });
    }

    #endregion

    #region Section 7.4: TryParse Function

    /// <summary>
    ///     Tests TryParse with valid text.
    /// </summary>
    [TestMethod]
    public void TryParse_ValidText_ShouldSucceed()
    {
        var query = (
            "\n"
            + "            text Record {\n"
            + "                Name: until ':'\n"
            + "            };\n"
            + "            select r.Name from #test.lines() l\n"
            + "            outer apply TryParse<Record>(l.Line) r\n"
            + "            where r.Name is not null"
        );

        var entities = new[]
        {
            new TextEntity { Name = "line1", Text = "Valid:data" },
            new TextEntity { Name = "line2", Text = "Also:valid" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
    }

    /// <summary>
    ///     Tests TryParse with missing delimiter.
    /// </summary>
    [TestMethod]
    public void TryParse_MissingDelimiter_ShouldReturnNull()
    {
        var query = (
            "\n"
            + "            text Record {\n"
            + "                Name: until ':'\n"
            + "            };\n"
            + "            select l.Line, r.Name from #test.lines() l\n"
            + "            outer apply TryParse<Record>(l.Line) r"
        );

        var entities = new[]
        {
            new TextEntity { Name = "line1", Text = "Has:colon" },
            new TextEntity { Name = "line2", Text = "NoColonHere" },
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Line", typeof(string)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Has:colon", "Has"],
            new object?[] { "NoColonHere", null }
        );
    }

    #endregion

    #region Section 7.5: InterpretAt Function

    /// <summary>
    ///     Tests InterpretAt with specific offset.
    /// </summary>
    [TestMethod]
    public void InterpretAt_WithOffset_ShouldStartAtPosition()
    {
        var query = (
            "\n"
            + "            binary Data {\n"
            + "                Value: int le\n"
            + "            };\n"
            + "            select d.Value from #test.bytes() b\n"
            + "            cross apply InterpretAt<Data>(b.Content, 4) d"
        );

        var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x0A, 0x00, 0x00, 0x00 };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Data = data },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(10, table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests InterpretAt with zero offset (same as Interpret).
    /// </summary>
    [TestMethod]
    public void InterpretAt_ZeroOffset_ShouldStartAtBeginning()
    {
        var query = (
            "\n"
            + "            binary Data {\n"
            + "                Value: int le\n"
            + "            };\n"
            + "            select d.Value from #test.bytes() b\n"
            + "            cross apply InterpretAt<Data>(b.Content, 0) d"
        );

        var data = "\n\u0000\u0000\u0000"u8.ToArray();
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Data = data },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(10, table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests InterpretAt with dynamic offset from field.
    /// </summary>
    [TestMethod]
    public void InterpretAt_DynamicOffset_ShouldUseFieldValue()
    {
        var query = (
            "\n"
            + "            binary Header {\n"
            + "                DataOffset: int le\n"
            + "            };\n"
            + "            binary Payload {\n"
            + "                Value: short le\n"
            + "            };\n"
            + "            select h.DataOffset, p.Value from #test.bytes() b\n"
            + "            cross apply Interpret<Header>(b.Content) h\n"
            + "            cross apply InterpretAt<Payload>(b.Content, h.DataOffset) p"
        );

        var data = new byte[] { 0x08, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x2A, 0x00 };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Data = data },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("DataOffset", typeof(int)), ("Value", typeof(short)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [8, (short)42]);
    }

    #endregion
}
