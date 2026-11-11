using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Binary Schema Primitive Types (Section 4.2 of specification).
///     Tests integer types with endianness and floating-point types.
/// </summary>
[TestClass]
public class BinaryPrimitivesFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Section 4.2.1: Integer Types - Single Byte

    /// <summary>
    ///     Tests byte parsing - single byte, no endianness needed.
    /// </summary>
    [TestMethod]
    public void Binary_Byte_ShouldParseWithoutEndianness()
    {
        var query = (
            "\n"
            + "            binary Data { Value: byte };\n"
            + "            select d.Value from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = new byte[] { 0xAB };
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
        Assert.AreEqual((byte)0xAB, table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests sbyte (signed byte) parsing.
    /// </summary>
    [TestMethod]
    public void Binary_SByte_ShouldParseNegativeValue()
    {
        var query = (
            "\n"
            + "            binary Data { Value: sbyte };\n"
            + "            select d.Value from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = new byte[] { 0xFF };
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
        Assert.AreEqual((sbyte)-1, table[0].ReadValue(0));
    }

    #endregion

    #region Section 4.2.1: Integer Types - Little Endian

    /// <summary>
    ///     Tests short (16-bit) little-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_ShortLittleEndian_ShouldParseCorrectly()
    {
        var query = (
            "\n"
            + "            binary Data { Value: short le };\n"
            + "            select d.Value from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = new byte[] { 0x34, 0x12 };
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
        Assert.AreEqual((short)0x1234, table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests ushort (unsigned 16-bit) little-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_UShortLittleEndian_ShouldParseHighValues()
    {
        var query = (
            "\n"
            + "            binary Data { Value: ushort le };\n"
            + "            select d.Value from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = new byte[] { 0xFF, 0xFF };
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
        Assert.AreEqual((ushort)65535, table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests int (32-bit) little-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_IntLittleEndian_ShouldParseCorrectly()
    {
        var query = (
            "\n"
            + "            binary Data { Value: int le };\n"
            + "            select d.Value from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = new byte[] { 0x78, 0x56, 0x34, 0x12 };
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
        Assert.AreEqual(0x12345678, table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests uint (unsigned 32-bit) little-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_UIntLittleEndian_ShouldParseHighValues()
    {
        var query = (
            "\n"
            + "            binary Data { Value: uint le };\n"
            + "            select d.Value from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
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
        Assert.AreEqual(uint.MaxValue, table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests long (64-bit) little-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_LongLittleEndian_ShouldParseCorrectly()
    {
        var query = (
            "\n"
            + "            binary Data { Value: long le };\n"
            + "            select d.Value from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 };
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
        Assert.AreEqual(0x0102030405060708L, table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests ulong (unsigned 64-bit) little-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_ULongLittleEndian_ShouldParseMaxValue()
    {
        var query = (
            "\n"
            + "            binary Data { Value: ulong le };\n"
            + "            select d.Value from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
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
        Assert.AreEqual(ulong.MaxValue, table[0].ReadValue(0));
    }

    #endregion

    #region Section 4.2.1: Integer Types - Big Endian

    /// <summary>
    ///     Tests short (16-bit) big-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_ShortBigEndian_ShouldParseCorrectly()
    {
        var query = (
            "\n"
            + "            binary Data { Value: short be };\n"
            + "            select d.Value from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = new byte[] { 0x12, 0x34 };
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
        Assert.AreEqual((short)0x1234, table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests int (32-bit) big-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_IntBigEndian_ShouldParseCorrectly()
    {
        var query = (
            "\n"
            + "            binary Data { Value: int be };\n"
            + "            select d.Value from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = new byte[] { 0x12, 0x34, 0x56, 0x78 };
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
        Assert.AreEqual(0x12345678, table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests long (64-bit) big-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_LongBigEndian_ShouldParseCorrectly()
    {
        var query = (
            "\n"
            + "            binary Data { Value: long be };\n"
            + "            select d.Value from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
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
        Assert.AreEqual(0x0102030405060708L, table[0].ReadValue(0));
    }

    #endregion

    #region Section 4.2.2: Floating-Point Types

    /// <summary>
    ///     Tests float (32-bit) little-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_FloatLittleEndian_ShouldParseCorrectly()
    {
        var query = (
            "\n"
            + "            binary Data { Value: float le };\n"
            + "            select d.Value from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = BitConverter.GetBytes(3.14f);
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
        Assert.AreEqual(3.14f, (float)table[0].ReadValue(0), 0.001f);
    }

    /// <summary>
    ///     Tests double (64-bit) little-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_DoubleLittleEndian_ShouldParseCorrectly()
    {
        var query = (
            "\n"
            + "            binary Data { Value: double le };\n"
            + "            select d.Value from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = BitConverter.GetBytes(3.141592653589793);
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
        Assert.AreEqual(3.141592653589793, (double)table[0].ReadValue(0), 0.0000001);
    }

    /// <summary>
    ///     Tests float big-endian parsing by reversing bytes.
    /// </summary>
    [TestMethod]
    public void Binary_FloatBigEndian_ShouldParseCorrectly()
    {
        var query = (
            "\n"
            + "            binary Data { Value: float be };\n"
            + "            select d.Value from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var leBytes = BitConverter.GetBytes(3.14f);
        Array.Reverse(leBytes);
        var entities = new[]
        {
            new BinaryEntity { Name = "test.bin", Content = leBytes },
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
        Assert.AreEqual(3.14f, (float)table[0].ReadValue(0), 0.001f);
    }

    #endregion

    #region Section 4.2.1: Multiple Primitive Fields

    /// <summary>
    ///     Tests parsing multiple primitive fields in sequence.
    /// </summary>
    [TestMethod]
    public void Binary_MultiplePrimitives_ShouldParseInSequence()
    {
        var query = (
            "\n"
            + "            binary Header {\n"
            + "                Magic: int le,\n"
            + "                Version: short le,\n"
            + "                Flags: byte,\n"
            + "                Reserved: byte\n"
            + "            };\n"
            + "            select h.Magic, h.Version, h.Flags, h.Reserved \n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Header>(f.Content) h"
        );

        var testData = new byte[] { 0x78, 0x56, 0x34, 0x12, 0x00, 0x01, 0xFF, 0x00 };
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
        Assert.AreEqual(0x12345678, table[0].ReadValue(0));
        Assert.AreEqual((short)256, table[0].ReadValue(1));
        Assert.AreEqual((byte)255, table[0].ReadValue(2));
        Assert.AreEqual((byte)0, table[0].ReadValue(3));
    }

    /// <summary>
    ///     Tests mixed endianness in same schema.
    /// </summary>
    [TestMethod]
    public void Binary_MixedEndianness_ShouldParseEachCorrectly()
    {
        var query = (
            "\n"
            + "            binary MixedData {\n"
            + "                LittleValue: int le,\n"
            + "                BigValue: int be\n"
            + "            };\n"
            + "            select m.LittleValue, m.BigValue \n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<MixedData>(f.Content) m"
        );

        var testData = new byte[] { 0x78, 0x56, 0x34, 0x12, 0x12, 0x34, 0x56, 0x78 };
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
        Assert.AreEqual(0x12345678, table[0].ReadValue(0));
        Assert.AreEqual(0x12345678, table[0].ReadValue(1));
    }

    #endregion

    #region Section 4.2.1: Negative Values

    /// <summary>
    ///     Tests parsing negative integer values.
    /// </summary>
    [TestMethod]
    public void Binary_NegativeInt_ShouldParseCorrectly()
    {
        var query = (
            "\n"
            + "            binary Data { Value: int le };\n"
            + "            select d.Value from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = BitConverter.GetBytes(-1);
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
        Assert.AreEqual(-1, table[0].ReadValue(0));
    }

    /// <summary>
    ///     Tests parsing minimum int value.
    /// </summary>
    [TestMethod]
    public void Binary_IntMinValue_ShouldParseCorrectly()
    {
        var query = (
            "\n"
            + "            binary Data { Value: int le };\n"
            + "            select d.Value from #test.files() f\n"
            + "            cross apply Interpret<Data>(f.Content) d"
        );

        var testData = BitConverter.GetBytes(int.MinValue);
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
        Assert.AreEqual(int.MinValue, table[0].ReadValue(0));
    }

    #endregion
}
