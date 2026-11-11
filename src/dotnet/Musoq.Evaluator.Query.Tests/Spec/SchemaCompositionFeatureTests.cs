using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Schema Composition (Section 8 of specification).
///     Tests schema references, 'as' clause chaining, and binary-text composition.
/// </summary>
[TestClass]
public class SchemaCompositionFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Section 8.2: As Clause Chaining

    /// <summary>
    ///     Tests extracting bytes then parsing as text.
    /// </summary>
    [TestMethod]
    public void SchemaComposition_BytesToText_AsClause()
    {
        var query = (
            "\n"
            + "            binary Container { \n"
            + "                Length: byte,\n"
            + "                TextData: byte[Length]\n"
            + "            };\n"
            + "            text Parsed { \n"
            + "                Key: until '=',\n"
            + "                Value: rest\n"
            + "            };\n"
            + "            select p.Key, p.Value from #test.bytes() b\n"
            + "            cross apply Interpret<Container>(b.Content) c\n"
            + "            cross apply Parse<Parsed>(ToText(c.TextData, 'utf-8')) p"
        );

        var data = "\tkey=value"u8.ToArray();
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
        Assert.AreEqual("key", table[0].ReadValue(0));
        Assert.AreEqual("value", table[0].ReadValue(1));
    }

    #endregion

    #region Section 8.3: Multiple Schema Types

    /// <summary>
    ///     Tests using both binary and text schemas in single query.
    /// </summary>
    [TestMethod]
    public void SchemaComposition_BinaryAndText_ShouldCombine()
    {
        var query = (
            "\n"
            + "            binary Header { \n"
            + "                Version: byte,\n"
            + "                RecordCount: int le\n"
            + "            };\n"
            + "            text Record { \n"
            + "                Name: until ','\n"
            + "            };\n"
            + "            select h.Version, h.RecordCount, r.Name \n"
            + "            from #btest.bytes() b\n"
            + "            cross apply Interpret<Header>(b.Content) h\n"
            + "            cross apply #ttest.lines() l\n"
            + "            cross apply Parse<Record>(l.Line) r"
        );

        var binaryData = new byte[] { 0x01, 0x02, 0x00, 0x00, 0x00 };
        var binaryEntities = new[]
        {
            new BinaryEntity { Name = "header.bin", Data = binaryData },
        };
        var textEntities = new[]
        {
            new TextEntity { Name = "row1", Text = "Alice,30" },
            new TextEntity { Name = "row2", Text = "Bob,25" },
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

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Section 8.4: Schema Reuse

    /// <summary>
    ///     Tests using same schema multiple times.
    /// </summary>
    [TestMethod]
    public void SchemaComposition_ReuseSchema_ShouldWork()
    {
        var query = (
            "\n"
            + "            binary Pair { \n"
            + "                A: int le,\n"
            + "                B: int le\n"
            + "            };\n"
            + "            select p1.A as First, p2.A as Second \n"
            + "            from #test.bytes() b1\n"
            + "            cross apply Interpret<Pair>(b1.Content) p1\n"
            + "            cross apply #test.bytes() b2\n"
            + "            cross apply Interpret<Pair>(b2.Content) p2\n"
            + "            where b1.Name <> b2.Name"
        );

        var data1 = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00 };
        var data2 = new byte[] { 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00 };
        var entities = new[]
        {
            new BinaryEntity { Name = "first.bin", Data = data1 },
            new BinaryEntity { Name = "second.bin", Data = data2 },
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

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Section 8.5: Schema with Conditional References

    /// <summary>
    ///     Tests schema reference with conditional fields.
    /// </summary>
    [TestMethod]
    public void SchemaComposition_ConditionalReference_ShouldWork()
    {
        var query = (
            "\n"
            + "            binary Extended { \n"
            + "                Extra: int le\n"
            + "            };\n"
            + "            binary Record { \n"
            + "                HasExtension: byte,\n"
            + "                Value: int le,\n"
            + "                Extension: Extended when HasExtension = 1\n"
            + "            };\n"
            + "            select r.HasExtension, r.Value, r.Extension.Extra from #test.bytes() b\n"
            + "            cross apply Interpret<Record>(b.Content) r\n"
            + "            where r.HasExtension = 1"
        );

        var data = new byte[] { 0x01, 0x0A, 0x00, 0x00, 0x00, 0x14, 0x00, 0x00, 0x00 };
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
        Assert.AreEqual((byte)1, table[0].ReadValue(0));
        Assert.AreEqual(10, table[0].ReadValue(1));
        Assert.AreEqual(20, table[0].ReadValue(2));
    }

    #endregion

    #region Section 8.6: Schema with Arrays of References

    /// <summary>
    ///     Tests array of referenced schemas.
    /// </summary>
    [TestMethod]
    public void SchemaComposition_ArrayOfReferences_ShouldWork()
    {
        var query = (
            "\n"
            + "            binary Point { \n"
            + "                X: short le,\n"
            + "                Y: short le\n"
            + "            };\n"
            + "            binary Polygon { \n"
            + "                VertexCount: byte,\n"
            + "                Vertices: Point[VertexCount]\n"
            + "            };\n"
            + "            select p.VertexCount, v.X, v.Y from #test.bytes() b\n"
            + "            cross apply Interpret<Polygon>(b.Content) p\n"
            + "            cross apply p.Vertices v\n"
            + "            order by v.X"
        );

        var data = new byte[] { 0x03, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x00, 0x05, 0x00, 0x0A, 0x00 };
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

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((byte)3, table[0].ReadValue(0));
    }

    #endregion

    #region Section 8.7: Text Schema Composition

    /// <summary>
    ///     Tests text schema referencing another text schema.
    /// </summary>
    [TestMethod]
    public void SchemaComposition_TextToText_ShouldWork()
    {
        var query = (
            "\n"
            + "            text KeyValue { \n"
            + "                Key: until '=',\n"
            + "                Value: rest\n"
            + "            };\n"
            + "            select kv.Key, kv.Value from #test.lines() l\n"
            + "            cross apply Parse<KeyValue>(l.Line) kv\n"
            + "            order by kv.Key"
        );

        var entities = new[]
        {
            new TextEntity { Name = "line1", Text = "name=John" },
            new TextEntity { Name = "line2", Text = "age=30" },
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
        Assert.AreEqual("age", table[0].ReadValue(0));
        Assert.AreEqual("30", table[0].ReadValue(1));
        Assert.AreEqual("name", table[1].ReadValue(0));
        Assert.AreEqual("John", table[1].ReadValue(1));
    }

    #endregion

    #region Section 8.1: Schema References

    /// <summary>
    ///     Tests referencing one schema from another.
    /// </summary>
    [TestMethod]
    public void SchemaComposition_BasicReference_ShouldWork()
    {
        var query = (
            "\n"
            + "            binary Point { \n"
            + "                X: int le,\n"
            + "                Y: int le\n"
            + "            };\n"
            + "            binary Rectangle { \n"
            + "                TopLeft: Point,\n"
            + "                BottomRight: Point\n"
            + "            };\n"
            + "            select r.TopLeft.X, r.TopLeft.Y, r.BottomRight.X, r.BottomRight.Y from #test.bytes() b\n"
            + "            cross apply Interpret<Rectangle>(b.Content) r"
        );

        var data = new byte[]
        {
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x64,
            0x00,
            0x00,
            0x00,
            0xC8,
            0x00,
            0x00,
            0x00,
        };
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
        Assert.AreEqual(0, table[0].ReadValue(0));
        Assert.AreEqual(0, table[0].ReadValue(1));
        Assert.AreEqual(100, table[0].ReadValue(2));
        Assert.AreEqual(200, table[0].ReadValue(3));
    }

    /// <summary>
    ///     Tests multiple levels of schema nesting.
    /// </summary>
    [TestMethod]
    public void SchemaComposition_DeepNesting_ShouldWork()
    {
        var query = (
            "\n"
            + "            binary Value { \n"
            + "                Data: int le\n"
            + "            };\n"
            + "            binary Wrapper { \n"
            + "                Inner: Value\n"
            + "            };\n"
            + "            binary Container { \n"
            + "                Wrapped: Wrapper\n"
            + "            };\n"
            + "            select c.Wrapped.Inner.Data from #test.bytes() b\n"
            + "            cross apply Interpret<Container>(b.Content) c"
        );

        var data = "*\u0000\u0000\u0000"u8.ToArray();
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
        Assert.AreEqual(42, table[0].ReadValue(0));
    }

    #endregion

    #region Section 8.8: Complex Composition Scenarios

    /// <summary>
    ///     Tests file format with header and data sections.
    /// </summary>
    [TestMethod]
    public void SchemaComposition_FileFormat_ShouldParseCorrectly()
    {
        var query = (
            "\n"
            + "            binary FileHeader { \n"
            + "                Magic: int le,\n"
            + "                Version: short le,\n"
            + "                DataOffset: short le\n"
            + "            };\n"
            + "            binary DataRecord { \n"
            + "                Id: int le,\n"
            + "                Value: int le\n"
            + "            };\n"
            + "            select h.Magic, h.Version, d.Id, d.Value \n"
            + "            from #test.bytes() b\n"
            + "            cross apply Interpret<FileHeader>(b.Content) h\n"
            + "            cross apply InterpretAt<DataRecord>(b.Content, h.DataOffset) d"
        );

        var data = new byte[]
        {
            0x4D,
            0x55,
            0x53,
            0x51,
            0x01,
            0x00,
            0x08,
            0x00,
            0x01,
            0x00,
            0x00,
            0x00,
            0x64,
            0x00,
            0x00,
            0x00,
        };
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
        Assert.AreEqual(0x5153554D, table[0].ReadValue(0));
        Assert.AreEqual((short)1, table[0].ReadValue(1));
        Assert.AreEqual(1, table[0].ReadValue(2));
        Assert.AreEqual(100, table[0].ReadValue(3));
    }

    /// <summary>
    ///     Tests schema composition with computed fields.
    /// </summary>
    [TestMethod]
    public void SchemaComposition_WithComputedFields_ShouldWork()
    {
        var query = (
            "\n"
            + "            binary Measurement { \n"
            + "                Raw: int le,\n"
            + "                Scaled: = Raw * 10\n"
            + "            };\n"
            + "            binary SensorData { \n"
            + "                SensorId: byte,\n"
            + "                Reading: Measurement\n"
            + "            };\n"
            + "            select s.SensorId, s.Reading.Raw, s.Reading.Scaled from #test.bytes() b\n"
            + "            cross apply Interpret<SensorData>(b.Content) s"
        );

        var data = new byte[] { 0x01, 0x05, 0x00, 0x00, 0x00 };
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
        Assert.AreEqual((byte)1, table[0].ReadValue(0));
        Assert.AreEqual(5, table[0].ReadValue(1));
        Assert.AreEqual(50, table[0].ReadValue(2));
    }

    #endregion
}
