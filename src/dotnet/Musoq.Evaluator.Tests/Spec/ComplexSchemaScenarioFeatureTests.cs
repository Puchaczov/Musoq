using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for complex real-world scenarios involving deeply nested schemas,
///     multi-level cross apply chains, schema composition with conditionals,
///     and realistic binary format parsing patterns.
/// </summary>
[TestClass]
public class ComplexSchemaScenarioFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Nested Schema with Computed from Inner Field

    /// <summary>
    ///     Tests nested schema where computed field in outer references inner schema fields.
    /// </summary>
    [TestMethod]
    public void Binary_NestedSchemaWithComputedFromInner_ShouldCompute()
    {
        var query = @"
            binary Point { X: byte, Y: byte };
            binary Shape { Id: byte, Origin: Point, Area: short le };
            select s.Id, s.Origin.X, s.Origin.Y, s.Area from #test.bytes() b
            cross apply Interpret(b.Content, 'Shape') s
            order by s.Id asc";

        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = [0x01, 0x0A, 0x14, 0x64, 0x00] },
            new BinaryEntity { Name = "2.bin", Data = [0x02, 0x1E, 0x28, 0xC8, 0x00] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((byte)10, table[0][1]);
        Assert.AreEqual((byte)20, table[0][2]);
        Assert.AreEqual((short)100, table[0][3]);
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual((byte)30, table[1][1]);
        Assert.AreEqual((byte)40, table[1][2]);
        Assert.AreEqual((short)200, table[1][3]);
    }

    #endregion

    #region Conditional Fields with Schema References

    /// <summary>
    ///     Tests conditional fields that contain schema references based on type discriminator.
    /// </summary>
    [TestMethod]
    public void Binary_ConditionalSchemaReference_ShouldSelectCorrectSchema()
    {
        var query = @"
            binary TextPayload { Len: byte, Text: string[Len] utf8 };
            binary IntPayload { Value: int le };
            binary Message {
                Type: byte,
                TextData: TextPayload when Type = 1,
                IntData: IntPayload when Type = 2
            };
            select m.Type, m.TextData.Text from #test.files() b
            cross apply Interpret(b.Content, 'Message') m
            where m.Type = 1";

        var text = Encoding.UTF8.GetBytes("Hi");
        var testData = new byte[] { 0x01, (byte)text.Length }
            .Concat(text)
            .ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual("Hi", table[0][1]);
    }

    #endregion

    #region Schema Array with Nested Schema References

    /// <summary>
    ///     Tests schema array where each element contains a nested schema reference.
    /// </summary>
    [TestMethod]
    public void Binary_ArrayOfSchemasWithNested_ShouldParseAll()
    {
        var query = @"
            binary Coord { X: byte, Y: byte };
            binary Shape { Id: byte, Origin: Coord };
            binary Canvas { ShapeCount: byte, Shapes: Shape[ShapeCount] };
            select s.Id, s.Origin.X, s.Origin.Y from #test.files() b
            cross apply Interpret(b.Content, 'Canvas') c
            cross apply c.Shapes s
            order by s.Id asc";

        var testData = new byte[]
        {
            0x02,
            0x01, 0x0A, 0x14,
            0x02, 0x1E, 0x28
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((byte)10, table[0][1]);
        Assert.AreEqual((byte)20, table[0][2]);
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual((byte)30, table[1][1]);
        Assert.AreEqual((byte)40, table[1][2]);
    }

    #endregion

    #region Multiple Cross Applies in Sequence

    /// <summary>
    ///     Tests two cross applies: file → schema → schema array.
    /// </summary>
    [TestMethod]
    public void Binary_SequentialCrossApply_ShouldChainCorrectly()
    {
        var query = @"
            binary Entry { Key: byte, Val: byte };
            binary DataBlock { Header: byte, RowCount: byte, Entries: Entry[RowCount] };
            select d.Header, r.Key, r.Val from #test.files() b
            cross apply Interpret(b.Content, 'DataBlock') d
            cross apply d.Entries r
            where d.Header = 0xAA
            order by r.Key asc";

        var testData = new byte[]
        {
            0xAA,
            0x03,
            0x03, 0x33,
            0x01, 0x11,
            0x02, 0x22
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((byte)0xAA, table[0][0]);
        Assert.AreEqual((byte)1, table[0][1]);
        Assert.AreEqual((byte)0x11, table[0][2]);
        Assert.AreEqual((byte)0xAA, table[1][0]);
        Assert.AreEqual((byte)2, table[1][1]);
        Assert.AreEqual((byte)0x22, table[1][2]);
        Assert.AreEqual((byte)0xAA, table[2][0]);
        Assert.AreEqual((byte)3, table[2][1]);
        Assert.AreEqual((byte)0x33, table[2][2]);
    }

    #endregion

    #region Computed Fields Chained Through Multiple Levels

    /// <summary>
    ///     Tests computed field that references another computed field.
    /// </summary>
    [TestMethod]
    public void Binary_ChainedComputedFields_ShouldResolveInOrder()
    {
        var query = @"
            binary Data { 
                Base: int le,
                Doubled: = Base * 2,
                Quadrupled: = Doubled * 2
            };
            select d.Base, d.Doubled, d.Quadrupled from #test.bytes() b
            cross apply Interpret(b.Content, 'Data') d";

        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = [0x05, 0x00, 0x00, 0x00] },
            new BinaryEntity { Name = "2.bin", Data = [0x0A, 0x00, 0x00, 0x00] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Schema with All Major Features Combined

    /// <summary>
    ///     Tests a schema using primitives, strings, byte arrays, and nested schemas together.
    /// </summary>
    [TestMethod]
    public void Binary_AllMajorFeaturesCombined_ShouldParseComplex()
    {
        var query = @"
            binary Record {
                Id: int le,
                NameLen: byte,
                Name: string[NameLen] utf8,
                Flags: byte,
                _: byte[2],
                Value: short le
            };
            select r.Id, r.Name, r.Flags, r.Value from #test.files() b
            cross apply Interpret(b.Content, 'Record') r";

        var name = Encoding.UTF8.GetBytes("test");
        var testData = new byte[] { 0x2A, 0x00, 0x00, 0x00 }
            .Concat([(byte)name.Length])
            .Concat(name)
            .Concat(new byte[] { 0x03, 0xFF, 0xFF, 0x64, 0x00 })
            .ToArray();

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0][0]);
        Assert.AreEqual("test", table[0][1]);
        Assert.AreEqual((byte)3, table[0][2]);
        Assert.AreEqual((short)100, table[0][3]);
    }

    #endregion

    #region Multi-File Cross Apply with GROUP BY

    /// <summary>
    ///     Tests processing multiple files, cross-applying arrays, and grouping results.
    /// </summary>
    [TestMethod]
    public void Binary_MultiFileCrossApplyGroupBy_ShouldAggregateAcrossFiles()
    {
        var query = @"
            binary Tag { Category: byte };
            binary File { TagCount: byte, Tags: Tag[TagCount] };
            select t.Category, Count(t.Category) as Cnt
            from #test.files() b
            cross apply Interpret(b.Content, 'File') f
            cross apply f.Tags t
            group by t.Category
            order by t.Category asc";

        var file1 = new byte[] { 0x03, 0x01, 0x02, 0x01 };
        var file2 = new byte[] { 0x02, 0x02, 0x03 };
        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Content = file1 },
            new BinaryEntity { Name = "2.bin", Content = file2 }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(2, table[0][1]);
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual((byte)3, table[2][0]);
        Assert.AreEqual(1, table[2][1]);
    }

    #endregion

    #region Text Schema Composition with Binary

    /// <summary>
    ///     Tests using MixedSchemaProvider to parse both binary and text in same query context.
    /// </summary>
    [TestMethod]
    public void Mixed_TextParsingWithMultipleLines_ShouldParseAllLines()
    {
        var query = @"
            text KeyVal { Key: until ':', Value: rest };
            select k.Key, k.Value from #test.lines() l
            cross apply Parse(l.Line, 'KeyVal') k
            where k.Key <> '#comment'
            order by k.Key asc";

        var textEntities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "host:localhost" },
            new TextEntity { Name = "2.txt", Text = "port:8080" },
            new TextEntity { Name = "3.txt", Text = "debug:true" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", textEntities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("debug", table[0][0]);
        Assert.AreEqual("true", table[0][1]);
        Assert.AreEqual("host", table[1][0]);
        Assert.AreEqual("localhost", table[1][1]);
        Assert.AreEqual("port", table[2][0]);
        Assert.AreEqual("8080", table[2][1]);
    }

    #endregion

    #region Schema Inheritance with Conditional in Child

    /// <summary>
    ///     Tests extended schema where child adds conditional field based on parent's field.
    /// </summary>
    [TestMethod]
    public void Binary_InheritanceWithConditionalChild_ShouldWork()
    {
        var query = @"
            binary Base { Type: byte, Size: byte };
            binary Extended extends Base {
                Data: byte[Size] when Type <> 0
            };
            select e.Type, e.Size from #test.files() b
            cross apply Interpret(b.Content, 'Extended') e
            where e.Type = 1";

        var testData = new byte[] { 0x01, 0x03, 0xAA, 0xBB, 0xCC };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((byte)3, table[0][1]);
    }

    #endregion
}
