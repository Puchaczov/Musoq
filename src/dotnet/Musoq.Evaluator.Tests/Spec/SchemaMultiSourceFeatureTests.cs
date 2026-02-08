using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for queries involving multiple data sources and complex
///     cross apply chains with schema interpretation.
///     Tests multi-file processing, double cross apply, and TryInterpret/TryParse usage.
/// </summary>
[TestClass]
public class SchemaMultiSourceFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region TryInterpret with NULL Handling

    /// <summary>
    ///     Tests TryInterpret returning NULL for invalid data and valid results for good data.
    /// </summary>
    [TestMethod]
    public void Binary_TryInterpret_ShouldReturnNullForInvalidData()
    {
        var query = @"
            binary Header { 
                Magic: int le check Magic = 305419896,
                Version: byte
            };
            select b.Name, d.Magic, d.Version from #test.files() b
            cross apply TryInterpret(b.Content, 'Header') d
            where d.Magic is not null
            order by b.Name asc";

        var validData = new byte[] { 0x78, 0x56, 0x34, 0x12, 0x02 };
        var invalidData = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01 };
        var entities = new[]
        {
            new BinaryEntity { Name = "good.bin", Content = validData },
            new BinaryEntity { Name = "bad.bin", Content = invalidData }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("good.bin", table[0][0]);
        Assert.AreEqual(0x12345678, table[0][1]);
        Assert.AreEqual((byte)2, table[0][2]);
    }

    #endregion

    #region TryParse with NULL Handling

    /// <summary>
    ///     Tests TryParse on text data where some lines match and some don't.
    /// </summary>
    [TestMethod]
    public void Text_TryParse_ShouldReturnNullForNonMatching()
    {
        var query = @"
            text KvPair { Key: until '=', Value: rest };
            select l.Name, d.Key, d.Value from #test.lines() l
            cross apply TryParse(l.Line, 'KvPair') d
            where d.Key is not null
            order by d.Key asc";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "host=localhost" },
            new TextEntity { Name = "2.txt", Text = "port=8080" },
            new TextEntity { Name = "3.txt", Text = "# this is a comment" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("host", table[0][1]);
        Assert.AreEqual("localhost", table[0][2]);
        Assert.AreEqual("port", table[1][1]);
        Assert.AreEqual("8080", table[1][2]);
    }

    #endregion

    #region Double Cross Apply with Nested Schema Array

    /// <summary>
    ///     Tests double cross apply: file → container → array elements.
    /// </summary>
    [TestMethod]
    public void Binary_DoubleCrossApply_ShouldFlattenNestedArrays()
    {
        var query = @"
            binary Point { X: byte, Y: byte };
            binary Shape { PointCount: byte, Points: Point[PointCount] };
            select p.X, p.Y from #test.files() b
            cross apply Interpret(b.Content, 'Shape') s
            cross apply s.Points p
            order by p.X asc, p.Y asc";

        var testData = new byte[]
        {
            0x03,
            0x01, 0x02,
            0x03, 0x04,
            0x05, 0x06
        };
        var entities = new[] { new BinaryEntity { Name = "shape.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((byte)2, table[0][1]);
        Assert.AreEqual((byte)3, table[1][0]);
        Assert.AreEqual((byte)4, table[1][1]);
        Assert.AreEqual((byte)5, table[2][0]);
        Assert.AreEqual((byte)6, table[2][1]);
    }

    #endregion

    #region Multiple Files with Same Schema

    /// <summary>
    ///     Tests processing multiple binary files each with the same schema.
    /// </summary>
    [TestMethod]
    public void Binary_MultipleFiles_ShouldParseEachFile()
    {
        var query = @"
            binary Header { Magic: int le, Version: byte };
            select b.Name, h.Magic, h.Version from #test.files() b
            cross apply Interpret(b.Content, 'Header') h
            order by b.Name asc";

        var file1 = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x0A };
        var file2 = new byte[] { 0x02, 0x00, 0x00, 0x00, 0x14 };
        var file3 = new byte[] { 0x03, 0x00, 0x00, 0x00, 0x1E };
        var entities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = file1 },
            new BinaryEntity { Name = "b.bin", Content = file2 },
            new BinaryEntity { Name = "c.bin", Content = file3 }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("a.bin", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual((byte)10, table[0][2]);
        Assert.AreEqual("b.bin", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual((byte)20, table[1][2]);
        Assert.AreEqual("c.bin", table[2][0]);
        Assert.AreEqual(3, table[2][1]);
        Assert.AreEqual((byte)30, table[2][2]);
    }

    #endregion

    #region Binary-Text Composition via Mixed Provider

    /// <summary>
    ///     Tests using MixedSchemaProvider to combine binary and text schemas in one query.
    /// </summary>
    [TestMethod]
    public void Mixed_BinaryAndText_ShouldWorkTogether()
    {
        var query = @"
            binary Header { Version: byte, Flags: byte };
            text Config { Key: until '=', Value: rest };
            select h.Version, h.Flags from #test.files() f
            cross apply Interpret(f.Content, 'Header') h
            order by h.Version asc";

        var binaryData = new byte[] { 0x01, 0xFF };
        var binaryEntities = new[]
        {
            new BinaryEntity { Name = "header.bin", Content = binaryData }
        };
        var textEntities = new[]
        {
            new TextEntity { Name = "config.txt", Text = "key=value" }
        };
        var schemaProvider = new MixedSchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", binaryEntities } },
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", textEntities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((byte)0xFF, table[0][1]);
    }

    #endregion

    #region Schema Array from Multiple Files with Aggregation

    /// <summary>
    ///     Tests aggregating schema array elements across multiple files.
    /// </summary>
    [TestMethod]
    public void Binary_MultiFileArrayAggregation_ShouldAggregateAcrossFiles()
    {
        var query = @"
            binary Item { Val: byte };
            binary Bag { Count: byte, Items: Item[Count] };
            select Sum(i.Val) as TotalVal, Count(i.Val) as TotalItems
            from #test.files() b
            cross apply Interpret(b.Content, 'Bag') bag
            cross apply bag.Items i";

        var file1 = new byte[] { 0x02, 0x0A, 0x14 };
        var file2 = new byte[] { 0x03, 0x01, 0x02, 0x03 };
        var entities = new[]
        {
            new BinaryEntity { Name = "bag1.bin", Content = file1 },
            new BinaryEntity { Name = "bag2.bin", Content = file2 }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(36m, table[0][0]);
        Assert.AreEqual(5, table[0][1]);
    }

    #endregion

    #region InterpretAt with Different Offsets

    /// <summary>
    ///     Tests InterpretAt to parse the same data at different offsets.
    /// </summary>
    [TestMethod]
    public void Binary_InterpretAt_ShouldParseAtSpecifiedOffset()
    {
        var query = @"
            binary Value { Data: int le };
            select d.Data from #test.files() b
            cross apply InterpretAt(b.Content, 4, 'Value') d";

        var testData = new byte[]
        {
            0xFF, 0xFF, 0xFF, 0xFF,
            0x2A, 0x00, 0x00, 0x00
        };
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

    #region Count Without GROUP BY

    /// <summary>
    ///     Tests counting all rows from schema interpretation without grouping.
    /// </summary>
    [TestMethod]
    public void Binary_CountAll_ShouldCountAllInterpretedRows()
    {
        var query = @"
            binary Record { Value: byte };
            select Count(d.Value) as Total from #test.bytes() b
            cross apply Interpret(b.Content, 'Record') d";

        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = [0x01] },
            new BinaryEntity { Name = "2.bin", Data = [0x02] },
            new BinaryEntity { Name = "3.bin", Data = [0x03] },
            new BinaryEntity { Name = "4.bin", Data = [0x04] },
            new BinaryEntity { Name = "5.bin", Data = [0x05] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(5, table[0][0]);
    }

    #endregion
}
