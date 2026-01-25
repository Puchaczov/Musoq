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
        var query = @"
            binary Container { 
                Length: byte,
                TextData: byte[Length]
            };
            text Parsed { 
                Key: until '=',
                Value: rest
            };
            select p.Key, p.Value from #test.bytes() b
            cross apply Interpret(b.Content, 'Container') c
            cross apply Parse(ToText(c.TextData, 'utf-8'), 'Parsed') p";


        var data = new byte[]
        {
            0x09,
            0x6B, 0x65, 0x79, 0x3D, 0x76, 0x61, 0x6C, 0x75, 0x65
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("key", table[0][0]);
        Assert.AreEqual("value", table[0][1]);
    }

    #endregion

    #region Section 8.3: Multiple Schema Types

    /// <summary>
    ///     Tests using both binary and text schemas in single query.
    /// </summary>
    [TestMethod]
    public void SchemaComposition_BinaryAndText_ShouldCombine()
    {
        var query = @"
            binary Header { 
                Version: byte,
                RecordCount: int le
            };
            text Record { 
                Name: until ','
            };
            select h.Version, h.RecordCount, r.Name 
            from #btest.bytes() b
            cross apply Interpret(b.Content, 'Header') h
            cross apply #ttest.lines() l
            cross apply Parse(l.Line, 'Record') r";

        var binaryData = new byte[]
        {
            0x01,
            0x02, 0x00, 0x00, 0x00
        };
        var binaryEntities = new[] { new BinaryEntity { Name = "header.bin", Data = binaryData } };
        var textEntities = new[]
        {
            new TextEntity { Name = "row1", Text = "Alice,30" },
            new TextEntity { Name = "row2", Text = "Bob,25" }
        };

        var schemaProvider = new MixedSchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#btest", binaryEntities } },
            new Dictionary<string, IEnumerable<TextEntity>> { { "#ttest", textEntities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
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
        var query = @"
            binary Pair { 
                A: int le,
                B: int le
            };
            select p1.A as First, p2.A as Second 
            from #test.bytes() b1
            cross apply Interpret(b1.Content, 'Pair') p1
            cross apply #test.bytes() b2
            cross apply Interpret(b2.Content, 'Pair') p2
            where b1.Name <> b2.Name";

        var data1 = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00 };
        var data2 = new byte[] { 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00 };
        var entities = new[]
        {
            new BinaryEntity { Name = "first.bin", Data = data1 },
            new BinaryEntity { Name = "second.bin", Data = data2 }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
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
        var query = @"
            binary Extended { 
                Extra: int le
            };
            binary Record { 
                HasExtension: byte,
                Value: int le,
                Extension: Extended when HasExtension = 1
            };
            select r.HasExtension, r.Value, r.Extension.Extra from #test.bytes() b
            cross apply Interpret(b.Content, 'Record') r
            where r.HasExtension = 1";

        var data = new byte[]
        {
            0x01,
            0x0A, 0x00, 0x00, 0x00,
            0x14, 0x00, 0x00, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(10, table[0][1]);
        Assert.AreEqual(20, table[0][2]);
    }

    #endregion

    #region Section 8.6: Schema with Arrays of References

    /// <summary>
    ///     Tests array of referenced schemas.
    /// </summary>
    [TestMethod]
    public void SchemaComposition_ArrayOfReferences_ShouldWork()
    {
        var query = @"
            binary Point { 
                X: short le,
                Y: short le
            };
            binary Polygon { 
                VertexCount: byte,
                Vertices: Point[VertexCount]
            };
            select p.VertexCount, v.X, v.Y from #test.bytes() b
            cross apply Interpret(b.Content, 'Polygon') p
            cross apply p.Vertices v
            order by v.X";

        var data = new byte[]
        {
            0x03,
            0x00, 0x00, 0x00, 0x00,
            0x0A, 0x00, 0x00, 0x00,
            0x05, 0x00, 0x0A, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((byte)3, table[0][0]);
    }

    #endregion

    #region Section 8.7: Text Schema Composition

    /// <summary>
    ///     Tests text schema referencing another text schema.
    /// </summary>
    [TestMethod]
    public void SchemaComposition_TextToText_ShouldWork()
    {
        var query = @"
            text KeyValue { 
                Key: until '=',
                Value: rest
            };
            select kv.Key, kv.Value from #test.lines() l
            cross apply Parse(l.Line, 'KeyValue') kv
            order by kv.Key";

        var entities = new[]
        {
            new TextEntity { Name = "line1", Text = "name=John" },
            new TextEntity { Name = "line2", Text = "age=30" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("age", table[0][0]);
        Assert.AreEqual("30", table[0][1]);
        Assert.AreEqual("name", table[1][0]);
        Assert.AreEqual("John", table[1][1]);
    }

    #endregion

    #region Section 8.1: Schema References

    /// <summary>
    ///     Tests referencing one schema from another.
    /// </summary>
    [TestMethod]
    public void SchemaComposition_BasicReference_ShouldWork()
    {
        var query = @"
            binary Point { 
                X: int le,
                Y: int le
            };
            binary Rectangle { 
                TopLeft: Point,
                BottomRight: Point
            };
            select r.TopLeft.X, r.TopLeft.Y, r.BottomRight.X, r.BottomRight.Y from #test.bytes() b
            cross apply Interpret(b.Content, 'Rectangle') r";

        var data = new byte[]
        {
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x64, 0x00, 0x00, 0x00,
            0xC8, 0x00, 0x00, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0, table[0][0]);
        Assert.AreEqual(0, table[0][1]);
        Assert.AreEqual(100, table[0][2]);
        Assert.AreEqual(200, table[0][3]);
    }

    /// <summary>
    ///     Tests multiple levels of schema nesting.
    /// </summary>
    [TestMethod]
    public void SchemaComposition_DeepNesting_ShouldWork()
    {
        var query = @"
            binary Value { 
                Data: int le
            };
            binary Wrapper { 
                Inner: Value
            };
            binary Container { 
                Wrapped: Wrapper
            };
            select c.Wrapped.Inner.Data from #test.bytes() b
            cross apply Interpret(b.Content, 'Container') c";

        var data = new byte[] { 0x2A, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0][0]);
    }

    #endregion

    #region Section 8.8: Complex Composition Scenarios

    /// <summary>
    ///     Tests file format with header and data sections.
    /// </summary>
    [TestMethod]
    public void SchemaComposition_FileFormat_ShouldParseCorrectly()
    {
        var query = @"
            binary FileHeader { 
                Magic: int le,
                Version: short le,
                DataOffset: short le
            };
            binary DataRecord { 
                Id: int le,
                Value: int le
            };
            select h.Magic, h.Version, d.Id, d.Value 
            from #test.bytes() b
            cross apply Interpret(b.Content, 'FileHeader') h
            cross apply InterpretAt(b.Content, h.DataOffset, 'DataRecord') d";

        var data = new byte[]
        {
            0x4D, 0x55, 0x53, 0x51,
            0x01, 0x00,
            0x08, 0x00,
            0x01, 0x00, 0x00, 0x00,
            0x64, 0x00, 0x00, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x5153554D, table[0][0]);
        Assert.AreEqual((short)1, table[0][1]);
        Assert.AreEqual(1, table[0][2]);
        Assert.AreEqual(100, table[0][3]);
    }

    /// <summary>
    ///     Tests schema composition with computed fields.
    /// </summary>
    [TestMethod]
    public void SchemaComposition_WithComputedFields_ShouldWork()
    {
        var query = @"
            binary Measurement { 
                Raw: int le,
                Scaled: = Raw * 10
            };
            binary SensorData { 
                SensorId: byte,
                Reading: Measurement
            };
            select s.SensorId, s.Reading.Raw, s.Reading.Scaled from #test.bytes() b
            cross apply Interpret(b.Content, 'SensorData') s";

        var data = new byte[]
        {
            0x01,
            0x05, 0x00, 0x00, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Data = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(5, table[0][1]);
        Assert.AreEqual(50, table[0][2]);
    }

    #endregion
}
