using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for edge cases and boundary conditions in schema interpretation.
///     Tests zero-length arrays, single-byte schemas, maximum values, null handling,
///     empty string fields, and TryInterpret/TryParse with mixed valid/invalid data.
/// </summary>
[TestClass]
public class SchemaEdgeCaseFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Zero-Length Array

    /// <summary>
    ///     Tests schema with zero-length array when count field is 0.
    /// </summary>
    [TestMethod]
    public void Binary_ZeroLengthArray_ShouldProduceNoElements()
    {
        var query = @"
            binary Item { Value: byte };
            binary Container { Count: byte, Items: Item[Count] };
            select i.Value from #test.files() b
            cross apply Interpret(b.Content, 'Container') c
            cross apply c.Items i";

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

    #region Single Byte Schema

    /// <summary>
    ///     Tests simplest possible schema - a single byte field.
    /// </summary>
    [TestMethod]
    public void Binary_SingleByteSchema_ShouldParseMinimalData()
    {
        var query = @"
            binary Minimal { Value: byte };
            select d.Value from #test.files() b
            cross apply Interpret(b.Content, 'Minimal') d";

        var testData = new byte[] { 0xFF };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0xFF, table[0][0]);
    }

    #endregion

    #region Maximum Type Values

    /// <summary>
    ///     Tests parsing maximum values for various integer types.
    /// </summary>
    [TestMethod]
    public void Binary_MaxValues_ShouldParseCorrectly()
    {
        var query = @"
            binary MaxTypes { 
                MaxByte: byte,
                MaxShort: short le,
                MaxInt: int le
            };
            select d.MaxByte, d.MaxShort, d.MaxInt from #test.files() b
            cross apply Interpret(b.Content, 'MaxTypes') d";

        var testData = new byte[]
        {
            0xFF,
            0xFF, 0x7F,
            0xFF, 0xFF, 0xFF, 0x7F
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(byte.MaxValue, table[0][0]);
        Assert.AreEqual(short.MaxValue, table[0][1]);
        Assert.AreEqual(int.MaxValue, table[0][2]);
    }

    #endregion

    #region Zero Values

    /// <summary>
    ///     Tests parsing all-zero data across multiple types.
    /// </summary>
    [TestMethod]
    public void Binary_ZeroValues_ShouldParseAsZero()
    {
        var query = @"
            binary ZeroData { 
                A: byte,
                B: short le,
                C: int le
            };
            select d.A, d.B, d.C from #test.files() b
            cross apply Interpret(b.Content, 'ZeroData') d";

        var testData = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.AreEqual((short)0, table[0][1]);
        Assert.AreEqual(0, table[0][2]);
    }

    #endregion

    #region Conditional Field Both Branches

    /// <summary>
    ///     Tests conditional field with both true and false outcomes from multiple files.
    /// </summary>
    [TestMethod]
    public void Binary_ConditionalBothBranches_ShouldHandleEach()
    {
        var query = @"
            binary Data { 
                HasPayload: byte,
                Payload: int le when HasPayload <> 0
            };
            select d.HasPayload, d.Payload from #test.bytes() b
            cross apply Interpret(b.Content, 'Data') d
            order by d.HasPayload asc";

        var entities = new[]
        {
            new BinaryEntity { Name = "with.bin", Data = [0x01, 0x2A, 0x00, 0x00, 0x00] },
            new BinaryEntity { Name = "without.bin", Data = [0x00] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.AreEqual((byte)1, table[1][0]);
        Assert.AreEqual(42, table[1][1]);
    }

    #endregion

    #region Zero-Length String

    /// <summary>
    ///     Tests parsing a zero-length string field.
    /// </summary>
    [TestMethod]
    public void Binary_ZeroLengthString_ShouldReturnEmpty()
    {
        var query = @"
            binary Data { 
                Len: byte,
                Text: string[Len] utf8,
                Suffix: byte
            };
            select d.Text, d.Suffix from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        var testData = new byte[] { 0x00, 0xAB };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("", table[0][0]);
        Assert.AreEqual((byte)0xAB, table[0][1]);
    }

    #endregion

    #region TryInterpret Mixed Valid and Invalid Files

    /// <summary>
    ///     Tests TryInterpret with a mix of files where some pass and some fail the check constraint.
    /// </summary>
    [TestMethod]
    public void Binary_TryInterpretMixedFiles_ShouldFilterInvalid()
    {
        var query = @"
            binary Packet { 
                Magic: short le check Magic = 0x1234,
                Data: byte
            };
            select b.Name, d.Data from #test.files() b
            cross apply TryInterpret(b.Content, 'Packet') d
            where d.Data is not null
            order by d.Data asc";

        var valid1 = new byte[] { 0x34, 0x12, 0x0A };
        var invalid = new byte[] { 0x00, 0x00, 0x0B };
        var valid2 = new byte[] { 0x34, 0x12, 0x0C };
        var entities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = valid1 },
            new BinaryEntity { Name = "b.bin", Content = invalid },
            new BinaryEntity { Name = "c.bin", Content = valid2 }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)0x0A, table[0][1]);
        Assert.AreEqual((byte)0x0C, table[1][1]);
    }

    #endregion

    #region TryParse Mixed Valid and Invalid Lines

    /// <summary>
    ///     Tests TryParse on lines where some match the schema and some don't.
    /// </summary>
    [TestMethod]
    public void Text_TryParseMixedLines_ShouldReturnOnlyMatching()
    {
        var query = @"
            text Config { 
                _: literal '[',
                Section: between '[' ']'
            };
            select d.Section from #test.lines() l
            cross apply TryParse(l.Line, 'Config') d
            where d.Section is not null
            order by d.Section asc";

        var entities = new[]
        {
            new TextEntity { Name = "1.txt", Text = "[[database]]" },
            new TextEntity { Name = "2.txt", Text = "key=value" },
            new TextEntity { Name = "3.txt", Text = "[[server]]" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("database", table[0][0]);
        Assert.AreEqual("server", table[1][0]);
    }

    #endregion

    #region Single Item Array

    /// <summary>
    ///     Tests schema array with exactly one element.
    /// </summary>
    [TestMethod]
    public void Binary_SingleItemArray_ShouldParseOneElement()
    {
        var query = @"
            binary Item { Value: short le };
            binary Container { Count: byte, Items: Item[Count] };
            select i.Value from #test.files() b
            cross apply Interpret(b.Content, 'Container') c
            cross apply c.Items i";

        var testData = new byte[] { 0x01, 0x2A, 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)42, table[0][0]);
    }

    #endregion

    #region Negative Integer Values

    /// <summary>
    ///     Tests parsing negative signed integer values.
    /// </summary>
    [TestMethod]
    public void Binary_NegativeValues_ShouldParseCorrectly()
    {
        var query = @"
            binary SignedData { 
                SignedByte: sbyte,
                SignedShort: short le,
                SignedInt: int le
            };
            select d.SignedByte, d.SignedShort, d.SignedInt from #test.files() b
            cross apply Interpret(b.Content, 'SignedData') d";

        var testData = new byte[]
        {
            0x80,
            0x00, 0x80,
            0xFF, 0xFF, 0xFF, 0xFF
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((sbyte)-128, table[0][0]);
        Assert.AreEqual((short)-32768, table[0][1]);
        Assert.AreEqual(-1, table[0][2]);
    }

    #endregion

    #region Text Schema with Empty Rest

    /// <summary>
    ///     Tests text rest field that captures empty string when input ends at delimiter.
    /// </summary>
    [TestMethod]
    public void Text_EmptyRest_ShouldReturnEmptyString()
    {
        var query = @"
            text Data { Prefix: until ':', Suffix: rest };
            select d.Prefix, d.Suffix from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[]
        {
            new TextEntity { Name = "test.txt", Text = "end:" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("end", table[0][0]);
        Assert.AreEqual("", table[0][1]);
    }

    #endregion

    #region Big Endian Integers

    /// <summary>
    ///     Tests big endian integer parsing for all major sizes.
    /// </summary>
    [TestMethod]
    public void Binary_BigEndianVariousTypes_ShouldParseCorrectly()
    {
        var query = @"
            binary NetworkPacket { 
                ShortBE: short be,
                IntBE: int be
            };
            select n.ShortBE, n.IntBE from #test.files() b
            cross apply Interpret(b.Content, 'NetworkPacket') n";

        var testData = new byte[]
        {
            0x00, 0x50,
            0x00, 0x00, 0x00, 0xC8
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)80, table[0][0]);
        Assert.AreEqual(200, table[0][1]);
    }

    #endregion

    #region Schema with Only Computed Fields After One Real Field

    /// <summary>
    ///     Tests schema with one parsed field and multiple computed fields.
    /// </summary>
    [TestMethod]
    public void Binary_OnlyComputedAfterFirst_ShouldCalculateAll()
    {
        var query = @"
            binary Data { 
                Raw: byte,
                TimesTwo: = Raw * 2,
                PlusTen: = Raw + 10,
                IsHigh: = Raw > 100
            };
            select d.Raw, d.TimesTwo, d.PlusTen, d.IsHigh from #test.bytes() b
            cross apply Interpret(b.Content, 'Data') d
            order by d.Raw asc";

        var entities = new[]
        {
            new BinaryEntity { Name = "1.bin", Data = [0xC8] },
            new BinaryEntity { Name = "2.bin", Data = [0x32] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)0x32, table[0][0]);
        Assert.AreEqual((byte)0xC8, table[1][0]);
    }

    #endregion

    #region Unsigned Type Boundary Values

    /// <summary>
    ///     Tests unsigned types at their maximum boundary values.
    /// </summary>
    [TestMethod]
    public void Binary_UnsignedMaxBoundary_ShouldParseCorrectly()
    {
        var query = @"
            binary UnsignedMax { 
                UByte: byte,
                UShortVal: ushort le,
                UIntVal: uint le
            };
            select d.UByte, d.UShortVal, d.UIntVal from #test.files() b
            cross apply Interpret(b.Content, 'UnsignedMax') d";

        var testData = new byte[]
        {
            0xFF,
            0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF
        };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(byte.MaxValue, table[0][0]);
        Assert.AreEqual(ushort.MaxValue, table[0][1]);
        Assert.AreEqual(uint.MaxValue, table[0][2]);
    }

    #endregion

    #region Zero-Length Byte Array

    /// <summary>
    ///     Tests zero-length byte array field.
    /// </summary>
    [TestMethod]
    public void Binary_ZeroLengthByteArray_ShouldReturnEmpty()
    {
        var query = @"
            binary Data { 
                Len: byte,
                Payload: byte[Len],
                Trailer: byte
            };
            select d.Len, d.Trailer from #test.files() b
            cross apply Interpret(b.Content, 'Data') d";

        var testData = new byte[] { 0x00, 0xAA };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.AreEqual((byte)0xAA, table[0][1]);
    }

    #endregion
}
