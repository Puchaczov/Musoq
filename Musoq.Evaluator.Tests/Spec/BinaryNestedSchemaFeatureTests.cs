using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Binary Schema Nested Types (Sections 4.3 and 4.4 of specification).
///     Tests nested schemas, schema arrays, and cross apply over arrays.
/// </summary>
[TestClass]
public class BinaryNestedSchemaFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Section 4.4: Dynamic Schema Arrays

    /// <summary>
    ///     Tests array of schemas with count from field.
    /// </summary>
    [TestMethod]
    public void Binary_SchemaArrayDynamic_ShouldParseBasedOnCount()
    {
        var query = @"
            binary Item { Value: int le };
            binary Container { 
                Count: byte,
                Items: Item[Count] 
            };
            select i.Value from #test.files() f
            cross apply Interpret(f.Content, 'Container') c
            cross apply c.Items i
            order by i.Value";


        var testData = new byte[9];
        testData[0] = 0x02;
        BitConverter.GetBytes(100).CopyTo(testData, 1);
        BitConverter.GetBytes(200).CopyTo(testData, 5);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(100, table[0][0]);
        Assert.AreEqual(200, table[1][0]);
    }

    #endregion

    #region Section 4.4: Primitive Arrays

    /// <summary>
    ///     Tests array of primitive integers.
    /// </summary>
    [TestMethod]
    public void Binary_PrimitiveArrayFixed_ShouldParseWithCrossApply()
    {
        var query = @"
            binary Data { 
                Count: byte,
                Values: int[Count] le 
            };
            select v.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d
            cross apply d.Values v
            order by v.Value";


        var testData = new byte[13];
        testData[0] = 0x03;
        BitConverter.GetBytes(10).CopyTo(testData, 1);
        BitConverter.GetBytes(20).CopyTo(testData, 5);
        BitConverter.GetBytes(30).CopyTo(testData, 9);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(10, table[0][0]);
        Assert.AreEqual(20, table[1][0]);
        Assert.AreEqual(30, table[2][0]);
    }

    #endregion

    #region Nested Schema with Arrays

    /// <summary>
    ///     Tests schema containing both nested schema and array.
    /// </summary>
    [TestMethod]
    public void Binary_NestedSchemaWithArray_ShouldParseBoth()
    {
        var query = @"
            binary Header { Magic: int le, Version: byte };
            binary Record { Id: short le };
            binary File { 
                Head: Header,
                RecordCount: byte,
                Records: Record[RecordCount]
            };
            select f2.Head.Magic, f2.Head.Version, r.Id 
            from #test.files() f
            cross apply Interpret(f.Content, 'File') f2
            cross apply f2.Records r
            order by r.Id";


        var testData = new byte[11];
        BitConverter.GetBytes(0xDEADBEEF).CopyTo(testData, 0);
        testData[4] = 0x01;
        testData[5] = 0x02;
        BitConverter.GetBytes((short)10).CopyTo(testData, 6);
        BitConverter.GetBytes((short)20).CopyTo(testData, 8);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);

        Assert.AreEqual(unchecked((int)0xDEADBEEF), table[0][0]);
        Assert.AreEqual((byte)1, table[0][1]);
        Assert.AreEqual((short)10, table[0][2]);
        Assert.AreEqual((short)20, table[1][2]);
    }

    #endregion

    #region Array Access Without Cross Apply

    /// <summary>
    ///     Tests accessing array element by index without cross apply.
    /// </summary>
    [TestMethod]
    public void Binary_ArrayIndexAccess_ShouldReturnElement()
    {
        var query = @"
            binary Point { X: int le, Y: int le };
            binary Triangle { Vertices: Point[3] };
            select 
                t.Vertices[0].X as X0,
                t.Vertices[0].Y as Y0,
                t.Vertices[1].X as X1,
                t.Vertices[2].Y as Y2
            from #test.files() f
            cross apply Interpret(f.Content, 'Triangle') t";


        var testData = new byte[24];
        for (var i = 0; i < 6; i++)
            BitConverter.GetBytes(i).CopyTo(testData, i * 4);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0, table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(2, table[0][2]);
        Assert.AreEqual(5, table[0][3]);
    }

    #endregion

    #region Section 4.3: Basic Nested Schema

    /// <summary>
    ///     Tests embedding one schema inside another.
    /// </summary>
    [TestMethod]
    public void Binary_NestedSchema_ShouldParseInline()
    {
        var query = @"
            binary Point { X: int le, Y: int le };
            binary Data { Origin: Point };
            select d.Origin.X, d.Origin.Y from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[8];
        BitConverter.GetBytes(100).CopyTo(testData, 0);
        BitConverter.GetBytes(200).CopyTo(testData, 4);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100, table[0][0]);
        Assert.AreEqual(200, table[0][1]);
    }

    /// <summary>
    ///     Tests multiple nested schemas at same level.
    /// </summary>
    [TestMethod]
    public void Binary_MultipleNestedSchemas_ShouldParseSequentially()
    {
        var query = @"
            binary Point { X: int le, Y: int le };
            binary Line { Start: Point, Finish: Point };
            select l.Start.X, l.Start.Y, l.Finish.X, l.Finish.Y 
            from #test.files() f
            cross apply Interpret(f.Content, 'Line') l";

        var testData = new byte[16];
        BitConverter.GetBytes(0).CopyTo(testData, 0);
        BitConverter.GetBytes(0).CopyTo(testData, 4);
        BitConverter.GetBytes(100).CopyTo(testData, 8);
        BitConverter.GetBytes(100).CopyTo(testData, 12);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0, table[0][0]);
        Assert.AreEqual(0, table[0][1]);
        Assert.AreEqual(100, table[0][2]);
        Assert.AreEqual(100, table[0][3]);
    }

    /// <summary>
    ///     Tests deeply nested schemas (3 levels).
    /// </summary>
    [TestMethod]
    public void Binary_DeeplyNestedSchema_ShouldParseAllLevels()
    {
        var query = @"
            binary Value { Data: int le };
            binary Container { Inner: Value };
            binary Wrapper { Middle: Container };
            select w.Middle.Inner.Data from #test.files() f
            cross apply Interpret(f.Content, 'Wrapper') w";

        var testData = BitConverter.GetBytes(42);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0][0]);
    }

    #endregion

    #region Section 4.4: Fixed-Size Schema Arrays

    /// <summary>
    ///     Tests array of schemas with fixed count.
    /// </summary>
    [TestMethod]
    public void Binary_SchemaArrayFixed_ShouldParseAllElements()
    {
        var query = @"
            binary Point { X: short le, Y: short le };
            binary Data { Points: Point[3] };
            select p.X, p.Y from #test.files() f
            cross apply Interpret(f.Content, 'Data') d
            cross apply d.Points p
            order by p.X";


        var testData = new byte[12];
        BitConverter.GetBytes((short)1).CopyTo(testData, 0);
        BitConverter.GetBytes((short)2).CopyTo(testData, 2);
        BitConverter.GetBytes((short)3).CopyTo(testData, 4);
        BitConverter.GetBytes((short)4).CopyTo(testData, 6);
        BitConverter.GetBytes((short)5).CopyTo(testData, 8);
        BitConverter.GetBytes((short)6).CopyTo(testData, 10);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((short)1, table[0][0]);
        Assert.AreEqual((short)2, table[0][1]);
        Assert.AreEqual((short)3, table[1][0]);
        Assert.AreEqual((short)4, table[1][1]);
        Assert.AreEqual((short)5, table[2][0]);
        Assert.AreEqual((short)6, table[2][1]);
    }

    /// <summary>
    ///     Tests empty schema array (count = 0).
    /// </summary>
    [TestMethod]
    public void Binary_SchemaArrayEmpty_ShouldReturnNoRows()
    {
        var query = @"
            binary Point { X: int le };
            binary Data { 
                Count: byte,
                Points: Point[Count] 
            };
            select p.X from #test.files() f
            cross apply Interpret(f.Content, 'Data') d
            cross apply d.Points p";

        var testData = new byte[] { 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(0, table.Count);
    }

    #endregion
}
