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
        var query = (
            "\n"
            + "            binary Item { Value: int le };\n"
            + "            binary Container { \n"
            + "                Count: byte,\n"
            + "                Items: Item[Count] \n"
            + "            };\n"
            + "            select i.Value from #test.files() f\n"
            + "            cross apply Interpret<Container>(f.Content) c\n"
            + "            cross apply c.Items i\n"
            + "            order by i.Value"
        );

        var testData = new byte[9];
        testData[0] = 0x02;
        BitConverter.GetBytes(100).CopyTo(testData, 1);
        BitConverter.GetBytes(200).CopyTo(testData, 5);

        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
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
        Assert.AreEqual(100, table[0].ReadValue(0));
        Assert.AreEqual(200, table[1].ReadValue(0));
    }

    #endregion

    #region Section 4.4: Primitive Arrays

    /// <summary>
    ///     Tests array of primitive integers.
    /// </summary>
    [TestMethod]
    public void Binary_PrimitiveArrayFixed_ShouldParseWithCrossApply()
    {
        var query = (
            "\n"
            + "            binary Data { \n"
            + "                Count: byte,\n"
            + "                Values: int[Count] le \n"
            + "            };\n"
            + "            select v.Value from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d\n"
            + "            cross apply d.Values v\n"
            + "            order by v.Value"
        );

        var testData = new byte[13];
        testData[0] = 0x03;
        BitConverter.GetBytes(10).CopyTo(testData, 1);
        BitConverter.GetBytes(20).CopyTo(testData, 5);
        BitConverter.GetBytes(30).CopyTo(testData, 9);

        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
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
        Assert.AreEqual(10, table[0].ReadValue(0));
        Assert.AreEqual(20, table[1].ReadValue(0));
        Assert.AreEqual(30, table[2].ReadValue(0));
    }

    #endregion

    #region Nested Schema with Arrays

    /// <summary>
    ///     Tests schema containing both nested schema and array.
    /// </summary>
    [TestMethod]
    public void Binary_NestedSchemaWithArray_ShouldParseBoth()
    {
        var query = (
            "\n"
            + "            binary Header { Magic: int le, Version: byte };\n"
            + "            binary Record { Id: short le };\n"
            + "            binary File { \n"
            + "                Head: Header,\n"
            + "                RecordCount: byte,\n"
            + "                Records: Record[RecordCount]\n"
            + "            };\n"
            + "            select f2.Head.Magic, f2.Head.Version, r.Id \n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<File>(f.Content) f2\n"
            + "            cross apply f2.Records r\n"
            + "            order by r.Id"
        );

        var testData = new byte[11];
        BitConverter.GetBytes(0xDEADBEEF).CopyTo(testData, 0);
        testData[4] = 0x01;
        testData[5] = 0x02;
        BitConverter.GetBytes((short)10).CopyTo(testData, 6);
        BitConverter.GetBytes((short)20).CopyTo(testData, 8);

        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
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

        Assert.AreEqual(unchecked((int)0xDEADBEEF), table[0].ReadValue(0));
        Assert.AreEqual((byte)1, table[0].ReadValue(1));
        Assert.AreEqual((short)10, table[0].ReadValue(2));
        Assert.AreEqual((short)20, table[1].ReadValue(2));
    }

    #endregion

    #region Array Access Without Cross Apply

    /// <summary>
    ///     Tests accessing array element by index without cross apply.
    /// </summary>
    [TestMethod]
    public void Binary_ArrayIndexAccess_ShouldReturnElement()
    {
        var query = (
            "\n"
            + "            binary Point { X: int le, Y: int le };\n"
            + "            binary Triangle { Vertices: Point[3] };\n"
            + "            select \n"
            + "                t.Vertices[0].X as X0,\n"
            + "                t.Vertices[0].Y as Y0,\n"
            + "                t.Vertices[1].X as X1,\n"
            + "                t.Vertices[2].Y as Y2\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Triangle>(f.Content) t"
        );

        var testData = new byte[24];
        for (var i = 0; i < 6; i++)
            BitConverter.GetBytes(i).CopyTo(testData, i * 4);

        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
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
        Assert.AreEqual(1, table[0].ReadValue(1));
        Assert.AreEqual(2, table[0].ReadValue(2));
        Assert.AreEqual(5, table[0].ReadValue(3));
    }

    #endregion

    #region Section 4.3: Basic Nested Schema

    /// <summary>
    ///     Tests embedding one schema inside another.
    /// </summary>
    [TestMethod]
    public void Binary_NestedSchema_ShouldParseInline()
    {
        var query = (
            "\n"
            + "            binary Point { X: int le, Y: int le };\n"
            + "            binary Data { Origin: Point };\n"
            + "            select d.Origin.X, d.Origin.Y from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = new byte[8];
        BitConverter.GetBytes(100).CopyTo(testData, 0);
        BitConverter.GetBytes(200).CopyTo(testData, 4);

        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
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
        Assert.AreEqual(100, table[0].ReadValue(0));
        Assert.AreEqual(200, table[0].ReadValue(1));
    }

    /// <summary>
    ///     Tests multiple nested schemas at same level.
    /// </summary>
    [TestMethod]
    public void Binary_MultipleNestedSchemas_ShouldParseSequentially()
    {
        var query = (
            "\n"
            + "            binary Point { X: int le, Y: int le };\n"
            + "            binary Line { Start: Point, Finish: Point };\n"
            + "            select l.Start.X, l.Start.Y, l.Finish.X, l.Finish.Y \n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Line>(f.Content) l"
        );

        var testData = new byte[16];
        BitConverter.GetBytes(0).CopyTo(testData, 0);
        BitConverter.GetBytes(0).CopyTo(testData, 4);
        BitConverter.GetBytes(100).CopyTo(testData, 8);
        BitConverter.GetBytes(100).CopyTo(testData, 12);

        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
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
        Assert.AreEqual(100, table[0].ReadValue(3));
    }

    /// <summary>
    ///     Tests deeply nested schemas (3 levels).
    /// </summary>
    [TestMethod]
    public void Binary_DeeplyNestedSchema_ShouldParseAllLevels()
    {
        var query = (
            "\n"
            + "            binary Value { Data: int le };\n"
            + "            binary Container { Inner: Value };\n"
            + "            binary Wrapper { Middle: Container };\n"
            + "            select w.Middle.Inner.Data from #test.files() f\n"
            + "            cross apply Interpret<Wrapper>(f.Content) w"
        );

        var testData = BitConverter.GetBytes(42);
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
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

    #region Section 4.4: Fixed-Size Schema Arrays

    /// <summary>
    ///     Tests array of schemas with fixed count.
    /// </summary>
    [TestMethod]
    public void Binary_SchemaArrayFixed_ShouldParseAllElements()
    {
        var query = (
            "\n"
            + "            binary Point { X: short le, Y: short le };\n"
            + "            binary Data { Points: Point[3] };\n"
            + "            select p.X, p.Y from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d\n"
            + "            cross apply d.Points p\n"
            + "            order by p.X"
        );

        var testData = new byte[12];
        BitConverter.GetBytes((short)1).CopyTo(testData, 0);
        BitConverter.GetBytes((short)2).CopyTo(testData, 2);
        BitConverter.GetBytes((short)3).CopyTo(testData, 4);
        BitConverter.GetBytes((short)4).CopyTo(testData, 6);
        BitConverter.GetBytes((short)5).CopyTo(testData, 8);
        BitConverter.GetBytes((short)6).CopyTo(testData, 10);

        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
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
        Assert.AreEqual((short)1, table[0].ReadValue(0));
        Assert.AreEqual((short)2, table[0].ReadValue(1));
        Assert.AreEqual((short)3, table[1].ReadValue(0));
        Assert.AreEqual((short)4, table[1].ReadValue(1));
        Assert.AreEqual((short)5, table[2].ReadValue(0));
        Assert.AreEqual((short)6, table[2].ReadValue(1));
    }

    /// <summary>
    ///     Tests empty schema array (count = 0).
    /// </summary>
    [TestMethod]
    public void Binary_SchemaArrayEmpty_ShouldReturnNoRows()
    {
        var query = (
            "\n"
            + "            binary Point { X: int le };\n"
            + "            binary Data { \n"
            + "                Count: byte,\n"
            + "                Points: Point[Count] \n"
            + "            };\n"
            + "            select p.X from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d\n"
            + "            cross apply d.Points p"
        );

        var testData = new byte[] { 0x00 };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
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

        Assert.AreEqual(0, table.Count);
    }

    #endregion
}
