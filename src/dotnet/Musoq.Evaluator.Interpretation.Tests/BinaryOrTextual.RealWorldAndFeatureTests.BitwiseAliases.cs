using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        var query = (
            "\n"
            + "            binary Header {\n"
            + "                PackedByte: byte\n"
            + "            };\n"
            + "            select\n"
            + "                h.PackedByte & 0x80 as HighBit\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Header>(f.Content) h"
        );

        var testData = new byte[] { 0xF7 };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x80L, table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests bitwise OR operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithBitwiseOrAlias_ShouldWork()
    {
        var query = (
            "\n"
            + "            binary Header {\n"
            + "                LowByte: byte,\n"
            + "                HighByte: byte\n"
            + "            };\n"
            + "            select\n"
            + "                h.LowByte | h.HighByte as Combined\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Header>(f.Content) h"
        );

        var testData = new byte[] { 0x0F, 0xF0 };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xFF, Convert.ToInt32(table[0].ReadValue(0)));
    }

    /// <summary>
    ///     Tests bitwise XOR operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithBitwiseXorAlias_ShouldWork()
    {
        var query = (
            "\n"
            + "            binary Header {\n"
            + "                A: byte,\n"
            + "                B: byte\n"
            + "            };\n"
            + "            select\n"
            + "                h.A ^ h.B as Xored\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Header>(f.Content) h"
        );

        var testData = new byte[] { 0xAA, 0x55 };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xFF, Convert.ToInt32(table[0].ReadValue(0)));
    }

    /// <summary>
    ///     Tests left shift operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithLeftShiftAlias_ShouldWork()
    {
        var query = (
            "\n"
            + "            binary Header {\n"
            + "                Value: byte\n"
            + "            };\n"
            + "            select\n"
            + "                h.Value << 4 as Shifted\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Header>(f.Content) h"
        );

        var testData = new byte[] { 0x0F };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xF0, Convert.ToInt32(table[0].ReadValue(0)));
    }

    /// <summary>
    ///     Tests right shift operation with alias in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithRightShiftAlias_ShouldWork()
    {
        var query = (
            "\n"
            + "            binary Header {\n"
            + "                Value: byte\n"
            + "            };\n"
            + "            select\n"
            + "                h.Value >> 4 as Shifted\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Header>(f.Content) h"
        );

        var testData = new byte[] { 0xF0 };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x0F, Convert.ToInt32(table[0].ReadValue(0)));
    }

    /// <summary>
    ///     Tests complex bitwise expression with alias in SELECT.
    ///     Note: This tests parentheses with bitwise and shift operators.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithComplexBitwiseExpressionAlias_ShouldWork()
    {
        var query = (
            "\n"
            + "            binary Header {\n"
            + "                PackedByte: byte\n"
            + "            };\n"
            + "            select\n"
            + "                h.PackedByte >> 4 as HighNibble,\n"
            + "                h.PackedByte & 0x0F as LowNibble\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Header>(f.Content) h"
        );

        var testData = new byte[] { 0xAB };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(0x0A, Convert.ToInt32(table[0].ReadValue(0)));
        Assert.AreEqual(0x0BL, table[0].ReadValue(1));
    }

    /// <summary>
    ///     Tests multiple bitwise aliases in a single SELECT.
    /// </summary>
    [TestMethod]
    public void Query_Select_WithMultipleBitwiseAliases_ShouldWork()
    {
        var query = (
            "\n"
            + "            binary GifFlags {\n"
            + "                PackedByte: byte\n"
            + "            };\n"
            + "            select\n"
            + "                h.PackedByte & 0x80 as HasGlobalColorTable,\n"
            + "                (h.PackedByte & 0x70) >> 4 as ColorResolution,\n"
            + "                h.PackedByte & 0x08 as SortFlag,\n"
            + "                h.PackedByte & 0x07 as SizeOfGlobalColorTable\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<GifFlags>(f.Content) h"
        );

        var testData = new byte[] { 0xF7 };
        var entities = new[]
        {
            new BinaryEntity { Name = "test.gif", Content = testData },
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x80L, table[0].ReadValue(0));
        Assert.AreEqual(0x07L, table[0].ReadValue(1));
        Assert.AreEqual(0x00L, table[0].ReadValue(2));
        Assert.AreEqual(0x07L, table[0].ReadValue(3));
    }

    #endregion
}
