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
        var query = @"
            binary Data { Value: byte };
            select d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = new byte[] { 0xAB };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0xAB, table[0][0]);
    }

    /// <summary>
    ///     Tests sbyte (signed byte) parsing.
    /// </summary>
    [TestMethod]
    public void Binary_SByte_ShouldParseNegativeValue()
    {
        var query = @"
            binary Data { Value: sbyte };
            select d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = new byte[] { 0xFF };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((sbyte)-1, table[0][0]);
    }

    #endregion

    #region Section 4.2.1: Integer Types - Little Endian

    /// <summary>
    ///     Tests short (16-bit) little-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_ShortLittleEndian_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: short le };
            select d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[] { 0x34, 0x12 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)0x1234, table[0][0]);
    }

    /// <summary>
    ///     Tests ushort (unsigned 16-bit) little-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_UShortLittleEndian_ShouldParseHighValues()
    {
        var query = @"
            binary Data { Value: ushort le };
            select d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[] { 0xFF, 0xFF };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((ushort)65535, table[0][0]);
    }

    /// <summary>
    ///     Tests int (32-bit) little-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_IntLittleEndian_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: int le };
            select d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[] { 0x78, 0x56, 0x34, 0x12 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x12345678, table[0][0]);
    }

    /// <summary>
    ///     Tests uint (unsigned 32-bit) little-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_UIntLittleEndian_ShouldParseHighValues()
    {
        var query = @"
            binary Data { Value: uint le };
            select d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(uint.MaxValue, table[0][0]);
    }

    /// <summary>
    ///     Tests long (64-bit) little-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_LongLittleEndian_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: long le };
            select d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x0102030405060708L, table[0][0]);
    }

    /// <summary>
    ///     Tests ulong (unsigned 64-bit) little-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_ULongLittleEndian_ShouldParseMaxValue()
    {
        var query = @"
            binary Data { Value: ulong le };
            select d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(ulong.MaxValue, table[0][0]);
    }

    #endregion

    #region Section 4.2.1: Integer Types - Big Endian

    /// <summary>
    ///     Tests short (16-bit) big-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_ShortBigEndian_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: short be };
            select d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[] { 0x12, 0x34 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)0x1234, table[0][0]);
    }

    /// <summary>
    ///     Tests int (32-bit) big-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_IntBigEndian_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: int be };
            select d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x12345678, table[0][0]);
    }

    /// <summary>
    ///     Tests long (64-bit) big-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_LongBigEndian_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: long be };
            select d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x0102030405060708L, table[0][0]);
    }

    #endregion

    #region Section 4.2.2: Floating-Point Types

    /// <summary>
    ///     Tests float (32-bit) little-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_FloatLittleEndian_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: float le };
            select d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = BitConverter.GetBytes(3.14f);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3.14f, (float)table[0][0], 0.001f);
    }

    /// <summary>
    ///     Tests double (64-bit) little-endian parsing.
    /// </summary>
    [TestMethod]
    public void Binary_DoubleLittleEndian_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: double le };
            select d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = BitConverter.GetBytes(3.141592653589793);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3.141592653589793, (double)table[0][0], 0.0000001);
    }

    /// <summary>
    ///     Tests float big-endian parsing by reversing bytes.
    /// </summary>
    [TestMethod]
    public void Binary_FloatBigEndian_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: float be };
            select d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var leBytes = BitConverter.GetBytes(3.14f);
        Array.Reverse(leBytes);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = leBytes } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3.14f, (float)table[0][0], 0.001f);
    }

    #endregion

    #region Section 4.2.1: Multiple Primitive Fields

    /// <summary>
    ///     Tests parsing multiple primitive fields in sequence.
    /// </summary>
    [TestMethod]
    public void Binary_MultiplePrimitives_ShouldParseInSequence()
    {
        var query = @"
            binary Header {
                Magic: int le,
                Version: short le,
                Flags: byte,
                Reserved: byte
            };
            select h.Magic, h.Version, h.Flags, h.Reserved 
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h";

        var testData = new byte[]
        {
            0x78, 0x56, 0x34, 0x12,
            0x00, 0x01,
            0xFF,
            0x00
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x12345678, table[0][0]);
        Assert.AreEqual((short)256, table[0][1]);
        Assert.AreEqual((byte)255, table[0][2]);
        Assert.AreEqual((byte)0, table[0][3]);
    }

    /// <summary>
    ///     Tests mixed endianness in same schema.
    /// </summary>
    [TestMethod]
    public void Binary_MixedEndianness_ShouldParseEachCorrectly()
    {
        var query = @"
            binary MixedData {
                LittleValue: int le,
                BigValue: int be
            };
            select m.LittleValue, m.BigValue 
            from #test.files() f
            cross apply Interpret(f.Content, 'MixedData') m";

        var testData = new byte[]
        {
            0x78, 0x56, 0x34, 0x12,
            0x12, 0x34, 0x56, 0x78
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x12345678, table[0][0]);
        Assert.AreEqual(0x12345678, table[0][1]);
    }

    #endregion

    #region Section 4.2.1: Negative Values

    /// <summary>
    ///     Tests parsing negative integer values.
    /// </summary>
    [TestMethod]
    public void Binary_NegativeInt_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: int le };
            select d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = BitConverter.GetBytes(-1);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(-1, table[0][0]);
    }

    /// <summary>
    ///     Tests parsing minimum int value.
    /// </summary>
    [TestMethod]
    public void Binary_IntMinValue_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: int le };
            select d.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = BitConverter.GetBytes(int.MinValue);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(int.MinValue, table[0][0]);
    }

    #endregion
}
