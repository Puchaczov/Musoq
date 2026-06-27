using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualRealWorldAndFeatureTests
{
    #region Bitwise Expression Alias Tests

    /// <summary>
    ///     Tests bitwise AND operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithBitwiseAndAlias_ShouldWork()
    {
        var query = @"
            binary Header {
                PackedByte: byte
            };
            select
                h.PackedByte & 0x80 as HighBit
            from #test.files() f
            cross apply Interpret<Header>(f.Content) h";

        var testData = new byte[] { 0xF7 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x80L, table[0][0]);
    }

    /// <summary>
    ///     Tests bitwise OR operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithBitwiseOrAlias_ShouldWork()
    {
        var query = @"
            binary Header {
                LowByte: byte,
                HighByte: byte
            };
            select
                h.LowByte | h.HighByte as Combined
            from #test.files() f
            cross apply Interpret<Header>(f.Content) h";

        var testData = new byte[] { 0x0F, 0xF0 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xFF, Convert.ToInt32(table[0][0]));
    }

    /// <summary>
    ///     Tests bitwise XOR operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithBitwiseXorAlias_ShouldWork()
    {
        var query = @"
            binary Header {
                A: byte,
                B: byte
            };
            select
                h.A ^ h.B as Xored
            from #test.files() f
            cross apply Interpret<Header>(f.Content) h";

        var testData = new byte[] { 0xAA, 0x55 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xFF, Convert.ToInt32(table[0][0]));
    }

    /// <summary>
    ///     Tests left shift operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithLeftShiftAlias_ShouldWork()
    {
        var query = @"
            binary Header {
                Value: byte
            };
            select
                h.Value << 4 as Shifted
            from #test.files() f
            cross apply Interpret<Header>(f.Content) h";

        var testData = new byte[] { 0x0F };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xF0, Convert.ToInt32(table[0][0]));
    }

    /// <summary>
    ///     Tests right shift operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithRightShiftAlias_ShouldWork()
    {
        var query = @"
            binary Header {
                Value: byte
            };
            select
                h.Value >> 4 as Shifted
            from #test.files() f
            cross apply Interpret<Header>(f.Content) h";

        var testData = new byte[] { 0xF0 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x0F, Convert.ToInt32(table[0][0]));
    }

    /// <summary>
    ///     Tests complex bitwise expression with alias in SELECT.
    ///     Note: This tests parentheses with bitwise and shift operators.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithComplexBitwiseExpressionAlias_ShouldWork()
    {
        var query = @"
            binary Header {
                PackedByte: byte
            };
            select
                h.PackedByte >> 4 as HighNibble,
                h.PackedByte & 0x0F as LowNibble
            from #test.files() f
            cross apply Interpret<Header>(f.Content) h";

        var testData = new byte[] { 0xAB };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(0x0A, Convert.ToInt32(table[0][0]));
        Assert.AreEqual(0x0BL, table[0][1]);
    }

    /// <summary>
    ///     Tests multiple bitwise aliases in a single SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithMultipleBitwiseAliases_ShouldWork()
    {
        var query = @"
            binary GifFlags {
                PackedByte: byte
            };
            select
                h.PackedByte & 0x80 as HasGlobalColorTable,
                (h.PackedByte & 0x70) >> 4 as ColorResolution,
                h.PackedByte & 0x08 as SortFlag,
                h.PackedByte & 0x07 as SizeOfGlobalColorTable
            from #test.files() f
            cross apply Interpret<GifFlags>(f.Content) h";


        var testData = new byte[] { 0xF7 };
        var entities = new[] { new BinaryEntity { Name = "test.gif", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x80L, table[0][0]);
        Assert.AreEqual(0x07L, table[0][1]);
        Assert.AreEqual(0x00L, table[0][2]);
        Assert.AreEqual(0x07L, table[0][3]);
    }

    #endregion
}
